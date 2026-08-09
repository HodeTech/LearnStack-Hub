using System.Buffers.Text;

namespace LearnStack.Hub.SharedKernel.Pagination;

/// <summary>
/// Encodes / decodes the opaque keyset cursor used by Hub list endpoints. The
/// cursor wraps the last-seen aggregate <see cref="Guid"/> as URL-safe base64;
/// the client treats it as opaque and never parses it.
/// </summary>
public static class CursorCodec
{
    /// <summary>Encodes a keyset id as an opaque cursor token.</summary>
    public static string Encode(Guid lastId) =>
        Base64Url.EncodeToString(lastId.ToByteArray());

    /// <summary>
    /// Decodes a cursor token to its keyset id. Returns <c>null</c> when the
    /// token is null/blank or malformed — callers treat that as "from the start".
    /// </summary>
    public static Guid? Decode(string? cursor)
    {
        if (string.IsNullOrWhiteSpace(cursor))
        {
            return null;
        }

        try
        {
            var bytes = Base64Url.DecodeFromChars(cursor);
            return bytes.Length == 16 ? new Guid(bytes) : null;
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
