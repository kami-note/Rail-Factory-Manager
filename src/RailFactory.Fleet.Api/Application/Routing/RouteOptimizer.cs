namespace RailFactory.Fleet.Api.Application.Routing;

/// <summary>
/// Greedy nearest-neighbor TSP route optimizer (RF-29).
/// O(n²) — suitable for up to ~50 stops without perceivable delay.
/// </summary>
public static class RouteOptimizer
{
    public static RouteOptimizationResult Optimize(IReadOnlyList<RouteStop> stops)
    {
        if (stops.Count <= 1)
            return new RouteOptimizationResult(stops.ToList(), 0m);

        var remaining = stops.ToList();
        var route = new List<RouteStop> { remaining[0] };
        remaining.RemoveAt(0);
        var totalKm = 0m;

        while (remaining.Count > 0)
        {
            var current = route[^1];
            var nearest = remaining.MinBy(s => HaversineKm(current.Lat, current.Lon, s.Lat, s.Lon))!;
            totalKm += HaversineKm(current.Lat, current.Lon, nearest.Lat, nearest.Lon);
            route.Add(nearest);
            remaining.Remove(nearest);
        }

        // Add return to start
        totalKm += HaversineKm(route[^1].Lat, route[^1].Lon, route[0].Lat, route[0].Lon);

        return new RouteOptimizationResult(route, Math.Round(totalKm, 2));
    }

    private static decimal HaversineKm(decimal lat1, decimal lon1, decimal lat2, decimal lon2)
    {
        const double R = 6371.0;
        var dLat = ToRad((double)(lat2 - lat1));
        var dLon = ToRad((double)(lon2 - lon1));
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2)
              + Math.Cos(ToRad((double)lat1)) * Math.Cos(ToRad((double)lat2))
              * Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return (decimal)(R * c);
    }

    private static double ToRad(double deg) => deg * Math.PI / 180.0;
}

public sealed record RouteStop(string Id, string Label, decimal Lat, decimal Lon);

public sealed record RouteOptimizationResult(
    List<RouteStop> OptimizedStops,
    decimal EstimatedDistanceKm);
