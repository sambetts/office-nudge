using System.ComponentModel.DataAnnotations.Schema;

namespace Entities.DB.Entities;

/// <summary>
/// User lookup for a session
/// </summary>
[Table("users")]
public class User : AbstractEFEntity
{
    [Column("upn")]
    public string UserPrincipalName { get; set; } = null!;

    [Column("azure_ad_id")]
    public string? AzureAdId { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"{UserPrincipalName}";
    }
}

