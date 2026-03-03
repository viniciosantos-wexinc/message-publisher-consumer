using System.Runtime.Serialization;
using System.Text.Json.Serialization;

namespace Messages.Models;

[DataContract(Name = nameof(AddressChangeDetected), Namespace = "wex.health.be.benefits")]
public record AddressChangeDetected
{
    [DataMember(Name = "user_id")]
    public long UserId { get; set; }

    [DataMember(Name = "address_id")]
    public long AddressID { get; set; }

    [DataMember(Name = "change_type_description")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public ChangeType ChangeType { get; set; }

    override public string ToString() =>
        $"AddressChangeDetected {{ UserId: {UserId}, AddressID: {AddressID}, ChangeType: {ChangeType} }}";
}
