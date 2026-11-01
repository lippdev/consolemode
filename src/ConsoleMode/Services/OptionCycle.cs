namespace ConsoleMode.Services;

/// <summary>Steps through a list of options the way the console interface's rows do. Pure so the tests can cover it.</summary>
public static class OptionCycle
{
    /// <summary>Index of the next (or previous) item, wrapping around; -1 for an empty list.</summary>
    public static int Cycle(int count, int current, int step)
    {
        if (count <= 0) return -1;
        if (current < 0) return step >= 0 ? 0 : count - 1;
        return ((current + step) % count + count) % count;
    }
}
