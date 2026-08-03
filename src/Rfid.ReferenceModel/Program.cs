using System.Text.Json;
using System.Text.Json.Serialization;
using Rfid.ReferenceModel;

var now = DateTimeOffset.Parse("2026-07-30T02:00:00Z");
var snapshot = new TagSnapshot(
    "demo-snapshot-0042",
    42,
    now.AddSeconds(-15),
    [new TagEntitlement("A1B2C3D4", "vehicle-demo-hatch", SubscriptionState.Active, 2, now.AddDays(30), now.AddSeconds(-30))]);
var read = new RfidRead("demo-read-001", "demo-decision-001", "A1B2C3D4", now, 4101);
var context = new DecisionContext(true, false, TimeSpan.FromMinutes(2), new Dictionary<string, DateTimeOffset>());
var decision = AccessDecisionEngine.Evaluate(read, snapshot, context);
var intent = OutputIntentPlanner.Plan(decision, OutputMode.DryRun, fieldCommissioned: false, now.AddSeconds(1));

var options = new JsonSerializerOptions
{
    WriteIndented = true,
    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    Converters = { new JsonStringEnumConverter(JsonNamingPolicy.SnakeCaseLower) }
};

Console.WriteLine(JsonSerializer.Serialize(new { decision, output_intent = intent }, options));
