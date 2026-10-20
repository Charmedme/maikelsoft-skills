# Personal data, secrets, and security events in logs

How to keep personal data and secrets out of logs, and still let support find a log entry. Decision SEC-14 (data classification) is in `dev-stack:secure-coding`. Ask it before the first log of personal data.

## Contents

- What never goes in a log
- The taxonomy
- Redaction setup
- Searchable values with HMAC
- Security events
- Limits

## What never goes in a log

- Secrets: passwords, tokens, API keys, connection strings, the HMAC key itself.
- Full request or response bodies and headers (they hold tokens and cookies).
- Raw personal data: email addresses, names, addresses, phone numbers, IP addresses where the taxonomy says so.

An ID that the service made (order ID, customer ID as a GUID) is not personal data by itself. Log it as a structured value.

## The taxonomy

Use the company taxonomy when one exists. Else use this small default (SEC-14): Public, Internal, Personal, Secret.

```csharp
public static class DataTaxonomy
{
    public static string TaxonomyName => "Orders";

    public static DataClassification Personal => new(TaxonomyName, nameof(Personal));

    public static DataClassification Secret => new(TaxonomyName, nameof(Secret));
}

[AttributeUsage(AttributeTargets.Parameter | AttributeTargets.Property | AttributeTargets.Field)]
public sealed class PersonalDataAttribute : DataClassificationAttribute
{
    public PersonalDataAttribute()
        : base(DataTaxonomy.Personal)
    {
    }
}
```

Put the attribute on the parameter of a `[LoggerMessage]` method (`[PersonalData] string email`), or on a property of a type that you log with `[LogProperties]`.

## Redaction setup

Packages: `Microsoft.Extensions.Telemetry` (`EnableRedaction`) and `Microsoft.Extensions.Compliance.Redaction` (`AddRedaction`), both 10.11.

```csharp
builder.Logging.EnableRedaction();
builder.Services.AddRedaction(redaction =>
{
    redaction.SetRedactor<ErasingRedactor>(new DataClassificationSet(DataTaxonomy.Secret));
    redaction.SetHmacRedactor(
        options =>
        {
            options.KeyId = 1;
            options.Key = builder.Configuration["Redaction:HmacKey"]!;
        },
        new DataClassificationSet(DataTaxonomy.Personal));
});
```

- `ErasingRedactor` writes an empty string. Checked: the message "Customer {Email} signed in" became "Customer  signed in".
- The redaction runs in the MEL logger factory (`ExtendedLoggerFactory`). Serilog as the factory (`AddSerilog`) skips it. Checked: with Serilog, the classified email was not redacted.
- Only classified values are redacted. A plain `string` parameter goes out as it is.

## Searchable values with HMAC

Support often searches by the email address. Give support the hash, not the raw value:

- `SetHmacRedactor` writes `<key id>:<base64 hash>`, for example `1:IWixH2XS+0EtSZ2PWRr/Bw==`. The same input and key give the same hash.
- Support computes the hash of the email with the same key (a small internal tool or a query function) and searches for it.
- The key is a base64 value of at least 44 characters, from the secret store. Use a different key for each major deployment (for example each region).
- Change `KeyId` when you rotate the key. An entry with another key ID has a hash of another key, so support computes the hash with the key of that ID.

## Security events

Decision SEC-23: security events are structured logs with stable event IDs. Give each event its own `[LoggerMessage]` method and an `EventId` in a reserved range (for example 9000 to 9099): sign-in failed, permission denied, token rejected, admin action. Log the user ID and the action, not the credentials. The backend keeps them for the retention that the business agreed.

## Limits

- Redaction does not see free text. A message template that puts the email in the text (`$"... {email}"`) leaks it. Rule 7 of `SKILL.md` (templates only) is a precondition.
- Exceptions: an exception message can hold personal data (for example a database constraint message with the value). Do not log `ex.Message` as a separate value; pass the exception, and keep personal data out of exception messages that you throw.
- Traces: span attributes are not redacted. Put only IDs on spans.
