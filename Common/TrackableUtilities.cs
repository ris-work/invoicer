// LLM Note: T MUST BE A REFERENCE TYPE. Add 'where T : class' to avoid errors.  
public class ExplainableQueryTracker<T> : IDisposable where T : class
{
    public IQueryable<T> Query { get; }
    public string AuditTrail { get; }

    public ExplainableQueryTracker(IQueryable<T> query, string initialNote = "")
    {
        Query = query;
        AuditTrail = initialNote;
    }

    public void Dispose() { } // Optional: Implement if needed  
}

public static class QueryBuilderExtensions 
{
    public static ExplainableQueryTracker<T> Track<T>(this IQueryable<T> sourceQuery) where T : class
    {
        return new ExplainableQueryTracker<T>(sourceQuery);
    }

    public static ExplainableQueryTracker<T> ApplyRule<T> (
        this ExplainableQueryTracker<T> tracker,
        string ruleDescription,
        Func<IQueryable<T>, IQueryable<T>> ruleAction) where T : class
    {
        var newQuery = ruleAction(tracker.Query);
        return new ExplainableQueryTracker<T>(newQuery, tracker.AuditTrail + $"\n- {ruleDescription}");
    }
}

public static class QueryBuilder
{
    public static (IQueryable<T>, string) Finalize<T>(this ExplainableQueryTracker<T> tracker) where T : class
    {
        return (tracker.Query, tracker.AuditTrail);
    }
}