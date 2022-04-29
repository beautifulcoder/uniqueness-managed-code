using System.Diagnostics;
using static System.Console;

WriteLine("Simple Distinct");
new[] {1, 1, 2, 3, 4, 5}.Distinct()
  .ToList()
  .ForEach(n => Write(n + " "));
WriteLine();
WriteLine();

const int uniqueEntries = 300;

var ratsA = 3000000.Times(x => new LabRatA
{
  Name = "LabRat_" + x % uniqueEntries,
  TrackingId = x % uniqueEntries,
  Color = (Color)(x % uniqueEntries)
}).ToList();

var ratsB = 3000000.Times(x => new LabRatB
(
  "LabRat_" + x % uniqueEntries,
  x % uniqueEntries,
  (Color)(x % uniqueEntries)
)).ToList();

var ratsC = 3000000.Times(x => new LabRatC
{
  Name = "LabRat_" + x % uniqueEntries,
  TrackingId = x % uniqueEntries,
  Color = (Color)(x % uniqueEntries)
}).ToList();

var stopWatch = new Stopwatch();

WriteLine("Bare Distinct");
stopWatch.Start();
WriteLine(ratsA.Distinct().Count());
stopWatch.Stop();
WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
WriteLine();

WriteLine("Naive Distinct");
stopWatch.Restart();
WriteLine(ratsA.Distinct(
    new LabRatANaiveComparer())
  .Count());
stopWatch.Stop();
WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
WriteLine();

WriteLine("Proper Distinct");
stopWatch.Restart();
WriteLine(ratsA.Distinct(
    new LabRatAProperComparer())
  .Count());
stopWatch.Stop();
WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
WriteLine();

WriteLine("Simple DistinctIterator");
stopWatch.Restart();
WriteLine(new DistinctIterator()
  .Distinct(ratsA)
  .Count());
stopWatch.Stop();
WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
WriteLine();

WriteLine("Async Distinct");
stopWatch.Restart();
WriteLine((await new []{"A", "B"}
    .SelectManyAsync(GetLabRats))
  .Distinct(new LabRatAProperComparer())
  .Count());
stopWatch.Stop();
WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
WriteLine();

WriteLine("Record Distinct");
stopWatch.Start();
WriteLine(ratsB.Distinct().Count());
stopWatch.Stop();
WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
WriteLine();

WriteLine("IEquatable Distinct");
stopWatch.Start();
WriteLine(ratsC.Distinct().Count());
stopWatch.Stop();
WriteLine($"{stopWatch.ElapsedMilliseconds} ms");
WriteLine();

Task<List<LabRatA>> GetLabRats(string type) => type switch
{
  "A" => Task.FromResult(ratsA),
  "B" => Task.FromResult(ratsA),
  _ => throw new ArgumentException("Invalid rat type")
};

public class DistinctIterator
{
  private readonly LabRatA?[] _entries;
  private readonly IEqualityComparer<LabRatA> _comparer;

  public DistinctIterator()
  {
    _entries = new LabRatA[300];
    _comparer = new LabRatAProperComparer();
  }

  public IEnumerable<LabRatA> Distinct(List<LabRatA> rats) =>
    rats.Where(AddIfNotPresent);

  private bool AddIfNotPresent(LabRatA rat)
  {
    var hashCode = _comparer.GetHashCode(rat);
    var bucket = GetBucketRef(hashCode);
    var i = (int)bucket;

    while (i >= 0)
    {
      var entry = _entries[i];
      if (entry != null &&
        _comparer.GetHashCode(entry) == hashCode &&
        _comparer.Equals(entry, rat))
      {
        return false;
      }

      i = -1;
    }

    if (_entries[bucket] != null) return false;

    _entries[bucket] = rat;

    return true;
  }

  private uint GetBucketRef(int hashCode) =>
    (uint)hashCode % (uint)_entries.Length;
}

public class LabRatANaiveComparer : EqualityComparer<LabRatA>
{
  public override bool Equals(LabRatA? x, LabRatA? y) =>
    x?.Name == y?.Name &&
    x?.TrackingId == y?.TrackingId &&
    x?.Color == y?.Color;

  public override int GetHashCode(LabRatA obj) => 1;
}

public class LabRatAProperComparer : EqualityComparer<LabRatA>
{
  public override bool Equals(LabRatA? x, LabRatA? y) =>
    x?.Name == y?.Name &&
    x?.TrackingId == y?.TrackingId &&
    x?.Color == y?.Color;

  public override int GetHashCode(LabRatA obj) =>
    (obj.Name,
    obj.TrackingId,
    obj.Color)
    .GetHashCode();
}

public enum Color
{
  Black,
  White,
  Brown
};

public class LabRatA
{
  public string Name { get; set; } = string.Empty;
  public int TrackingId { get; set; }
  public Color Color { get; set; }
}

public record LabRatB(string Name, int TrackingId, Color Color);

public class LabRatC : IEquatable<LabRatC>
{
  protected virtual Type EqualityContract => typeof(LabRatC);

  public string Name { get; init; } = string.Empty;
  public int TrackingId { get; init; }
  public Color Color { get; init; }

  public override int GetHashCode() =>
    HashCode.Combine(
      EqualityComparer<Type>.Default.GetHashCode(EqualityContract),
      Name.GetHashCode(),
      TrackingId.GetHashCode(),
      Color.GetHashCode());

  public bool Equals(LabRatC? other) =>
    Name == other?.Name &&
    TrackingId == other.TrackingId &&
    Color == other.Color;
}

public static class EnumerableExtensions
{
  public static IEnumerable<T> Times<T>(
    this int count, Func<int, T> func)
  {
    for (var i = 1; i <= count; i++) yield return func(i);
  }

  public static async Task<IEnumerable<TR>> SelectManyAsync<T, TR>(
    this IEnumerable<T> enumeration,
    Func<T, Task<List<TR>>> func) =>
    (await Task.WhenAll(enumeration.Select(func))
        .ConfigureAwait(false))
      .SelectMany(s => s);
}
