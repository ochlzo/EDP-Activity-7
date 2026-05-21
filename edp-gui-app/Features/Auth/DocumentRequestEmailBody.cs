using System.Text;

namespace edp_gui_app;

public static class DocumentRequestEmailBody
{
    public static string Build(DocumentRequestEmailMessage message)
    {
        var body = new StringBuilder();
        body.AppendLine($"Hello {message.TenantName},");
        body.AppendLine();
        body.AppendLine("Your site owner requested the following documents:");
        foreach (var document in message.RequestedDocuments)
        {
            body.AppendLine($"- {document}");
        }

        body.AppendLine();
        body.AppendLine("Open this local document request form to upload the files:");
        body.AppendLine(message.RequestUrl);
        body.AppendLine();
        body.AppendLine("This link is intended for same-machine testing while the owner app is running.");
        return body.ToString();
    }
}
