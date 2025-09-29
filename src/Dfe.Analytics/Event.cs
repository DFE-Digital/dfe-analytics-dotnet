using System.Diagnostics.CodeAnalysis;
using System.Security.Cryptography;
using System.Text;
using Google.Cloud.BigQuery.V2;

namespace Dfe.Analytics;

/// <summary>
/// Represents a DfE Analytics event.
/// </summary>
public class Event
{
    private string? _environment;

    /// <summary>
    /// Gets or sets the <c>occurred_at</c> field.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Gets the <c>event_type</c> field.
    /// </summary>
    public string EventType { get; } = "web_request";

    /// <summary>
    /// Gets or sets the <c>environment</c> field.
    /// </summary>
    /// <remarks>
    /// Cannot be <see langword="null"/> or empty.
    /// </remarks>
    [DisallowNull]
    public string? Environment
    {
        get => _environment;
        set
        {
            ArgumentException.ThrowIfNullOrEmpty(value);
            _environment = value;
        }
    }

    /// <summary>
    /// Gets or sets the <c>namespace</c> field.
    /// </summary>
    public string? Namespace { get; set; }

    /// <summary>
    /// Gets or set the <c>user_id</c> field.
    /// </summary>
    public string? UserId { get; set; }

    /// <summary>
    /// Gets or set the <c>request_uuid</c> field.
    /// </summary>
    public string? RequestId { get; set; }

    /// <summary>
    /// Gets or sets the <c>request_method</c> field.
    /// </summary>
    public string? RequestMethod { get; set; }

    /// <summary>
    /// Gets or sets the <c>request_path</c> field.
    /// </summary>
    public string? RequestPath { get; set; }

    /// <summary>
    /// Gets or sets the <c>request_user_agent</c> field.
    /// </summary>
    public string? RequestUserAgent { get; set; }

    /// <summary>
    /// Gets or sets the <c>request_referer</c> field.
    /// </summary>
    public string? RequestReferer { get; set; }

    /// <summary>
    /// Gets or sets the <c>request_query</c> field.
    /// </summary>
    public IDictionary<string, string[]>? RequestQuery { get; set; }

    /// <summary>
    /// Gets or sets the <c>response_content_type</c> field.
    /// </summary>
    public string? ResponseContentType { get; set; }

    /// <summary>
    /// Gets or sets the <c>response_status</c> field.
    /// </summary>
    public string? ResponseStatus { get; set; }

    /// <summary>
    /// Gets the <c>data</c> field.
    /// </summary>
    public IDictionary<string, List<string>> Data { get; } = new Dictionary<string, List<string>>();

    /// <summary>
    /// Gets or sets the <c>entity_table_name</c> field.
    /// </summary>
    public string? EntityTableName { get; set; }

    /// <summary>
    /// Gets or sets the <c>anonymised_user_agent_and_ip</c> field.
    /// </summary>
    public string? AnonymizedUserAgentAndIp { get; set; }

    /// <summary>
    /// Gets the <c>event_tags</c> field.
    /// </summary>
    public ICollection<string> Tags { get; set; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Adds an item to the <see cref="Data"/> bundle with a single value.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="value"/> is <see langword="null"/>.</exception>
    public void AddData(string key, string value)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(value);

        AddData(key, [value]);
    }

    /// <summary>
    /// Adds an item to the <see cref="Data"/> bundle with multiple values.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
    public void AddData(string key, IEnumerable<string> values)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(values);

        Data.Add(key, [.. values]);
    }

    /// <summary>
    /// Adds an item to the <see cref="Data"/> bundle with multiple values.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="key"/> or <paramref name="values"/> is <see langword="null"/>.</exception>
    public void AddData(string key, params string[] values)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(values);

        AddData(key, (IEnumerable<string>)values);
    }

    /// <summary>
    /// Adds a tag to the <see cref="Tags"/> collection.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="tag"/> is <see langword="null" />.</exception>
    public void AddTag(string tag)
    {
        ArgumentNullException.ThrowIfNull(tag);

        Tags.Add(tag);
    }

    /// <summary>
    /// Adds tags to the <see cref="Tags"/> collection.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="tags"/> is <see langword="null" />.</exception>
    public void AddTags(IEnumerable<string> tags)
    {
        ArgumentNullException.ThrowIfNull(tags);

        foreach (var tag in tags)
        {
            Tags.Add(tag);
        }
    }

    /// <summary>
    /// Adds tags to the <see cref="Tags"/> collection.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="tags"/> is <see langword="null" />.</exception>
    public void AddTags(params string[] tags) => AddTags((IEnumerable<string>)tags);

    /// <summary>
    /// Creates a <see cref="BigQueryInsertRow"/> with the data from this event.
    /// </summary>
    public BigQueryInsertRow ToBigQueryInsertRow() =>
        new()
        {
            { "occurred_at", OccurredAt },
            { "event_type", EventType },
            { "environment", Environment },
            { "namespace", Namespace },
            { "user_id", UserId },
            { "request_uuid", RequestId },
            { "request_method", RequestMethod },
            { "request_path", RequestPath },
            { "request_user_agent", RequestUserAgent },
            { "request_referer", RequestReferer },
            {
                "request_query",
                RequestQuery?.Select(qp => new BigQueryInsertRow()
                {
                    { "key", qp.Key },
                    { "value", qp.Value }
                })?.ToArray() ?? []
            },
            { "response_content_type", ResponseContentType },
            { "response_status", ResponseStatus },
            {
                "data",
                Data.Select(kvp => new BigQueryInsertRow()
                {
                    { "key", kvp.Key },
                    { "value", kvp.Value }
                }).ToArray()
            },
            { "entity_table_name", EntityTableName },
            { "anonymised_user_agent_and_ip", AnonymizedUserAgentAndIp },
            { "event_tags", Tags.ToArray() },
        };

    internal static string Anonymize(string value)
    {
        // https://github.com/DFE-Digital/dfe-analytics/blob/8a181ff385810dbe7c7bb7fec5a55033a7b1fad0/lib/dfe/analytics.rb#L149-L151
        var hashBytes = SHA256.HashData(Encoding.UTF8.GetBytes(value));
#pragma warning disable CA1308
        return Convert.ToHexString(hashBytes).ToLowerInvariant();
#pragma warning restore CA1308
    }
}
