namespace Orders.Api.Documents;

// The shared look of our printed documents: the dark blue header bar and the table style.
public static class DocumentStyles
{
    public const string Css = """
        body { font-family: 'Inter', Arial, sans-serif; font-size: 11pt; color: #1d1d1f; }
        .header { background: #0b3d91; color: #ffffff; padding: 12px 16px; display: flex; justify-content: space-between; align-items: center; }
        .header img { height: 32px; }
        table { width: 100%; border-collapse: collapse; margin-top: 16px; }
        th { text-align: left; border-bottom: 2px solid #0b3d91; padding: 4px; }
        td { border-bottom: 1px solid #d0d0d0; padding: 4px; }
        tr:nth-child(even) td { background: #f2f5fb; }
        .note { white-space: pre-wrap; border: 1px solid #d0d0d0; padding: 8px; margin-top: 16px; }
        .footer { margin-top: 24px; font-size: 9pt; color: #6e6e73; }
        """;

    public const string FontLink = """<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Inter:wght@400;600&display=swap">""";
}
