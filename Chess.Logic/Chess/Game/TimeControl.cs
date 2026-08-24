namespace Chess.Logic;

public readonly record struct TimeControl(TimeSpan InitialTime, TimeSpan Increment)
{
    public static TimeControl Create(TimeSpan initialTime, TimeSpan increment)
    {
        if (initialTime <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(initialTime), initialTime, "Initial time must be positive.");
        if (increment < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(increment), increment, "Increment cannot be negative.");

        return new TimeControl(initialTime, increment);
    }
}
