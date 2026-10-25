namespace Orders.Api.Customers;

// A logo image as it goes into a document: the bytes and the media type (image/png or image/jpeg).
public sealed record LogoImage(byte[] Bytes, string ContentType)
{
    public string DataUri => $"data:{ContentType};base64,{Convert.ToBase64String(Bytes)}";
}
