using System.Runtime.Serialization;

namespace Messages.Models;

[DataContract(Name = nameof(SubscribedToReclaim), Namespace = "EmployeeDataStream.Models.Kafka")]
public record SubscribedToReclaim
{
    [DataMember(Name = "client_id")]
    public int client_id { get; set; }

    [DataMember(Name = "user_id")]
    public int user_id { get; set; }

    [DataMember(Name = "reclaim_batch_id")]
    public int reclaim_batch_id { get; set; }

    [DataMember(Name = "successfully_subscribed")]
    public bool successfully_subscribed { get; set; }

    [DataMember(Name = "errors")]
    public string? errors { get; set; }

    public override string ToString() =>
        $"SubscribedToReclaim {{ ClientID: {client_id}, UserID: {user_id}, ReclaimBatchID: {reclaim_batch_id}, SuccessfullySubscribed: {successfully_subscribed}, Errors: {errors} }}";
}