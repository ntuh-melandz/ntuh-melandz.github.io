using System.Text.Json.Serialization;

namespace SmartOnFhirApp.Models;

// SMART Configuration from .well-known endpoint
public class SmartConfiguration
{
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("capabilities")]
    public List<string> Capabilities { get; set; } = new();
}

// Token Response
public class TokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("token_type")]
    public string TokenType { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("scope")]
    public string Scope { get; set; } = string.Empty;

    [JsonPropertyName("patient")]
    public string? Patient { get; set; }

    [JsonPropertyName("id_token")]
    public string? IdToken { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}

// FHIR Patient Resource
public class Patient
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "Patient";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("identifier")]
    public List<Identifier>? Identifier { get; set; }

    [JsonPropertyName("name")]
    public List<HumanName>? Name { get; set; }

    [JsonPropertyName("gender")]
    public string? Gender { get; set; }

    [JsonPropertyName("birthDate")]
    public string? BirthDate { get; set; }

    [JsonPropertyName("address")]
    public List<Address>? Address { get; set; }

    [JsonPropertyName("telecom")]
    public List<ContactPoint>? Telecom { get; set; }

    [JsonPropertyName("managingOrganization")]
    public Reference? ManagingOrganization { get; set; }
}

public class Organization
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "Organization";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("name")]
    public string? Name { get; set; }

    [JsonPropertyName("type")]
    public List<CodeableConcept>? Type { get; set; }

    [JsonPropertyName("telecom")]
    public List<ContactPoint>? Telecom { get; set; }

    [JsonPropertyName("address")]
    public List<Address>? Address { get; set; }
}

public class Identifier
{
    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }
}

public class HumanName
{
    [JsonPropertyName("use")]
    public string? Use { get; set; }

    [JsonPropertyName("family")]
    public string? Family { get; set; }

    [JsonPropertyName("given")]
    public List<string>? Given { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class Address
{
    [JsonPropertyName("use")]
    public string? Use { get; set; }

    [JsonPropertyName("line")]
    public List<string>? Line { get; set; }

    [JsonPropertyName("city")]
    public string? City { get; set; }

    [JsonPropertyName("postalCode")]
    public string? PostalCode { get; set; }

    [JsonPropertyName("country")]
    public string? Country { get; set; }
}

public class ContactPoint
{
    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("value")]
    public string? Value { get; set; }

    [JsonPropertyName("use")]
    public string? Use { get; set; }
}

// FHIR Observation Resource
public class Observation
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "Observation";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("code")]
    public CodeableConcept? Code { get; set; }

    [JsonPropertyName("subject")]
    public Reference? Subject { get; set; }

    [JsonPropertyName("effectiveDateTime")]
    public string? EffectiveDateTime { get; set; }

    [JsonPropertyName("valueQuantity")]
    public Quantity? ValueQuantity { get; set; }

    [JsonPropertyName("component")]
    public List<ObservationComponent>? Component { get; set; }
}

public class ObservationComponent
{
    [JsonPropertyName("code")]
    public CodeableConcept? Code { get; set; }

    [JsonPropertyName("valueQuantity")]
    public Quantity? ValueQuantity { get; set; }
}

// FHIR Condition Resource
public class Condition
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "Condition";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("clinicalStatus")]
    public CodeableConcept? ClinicalStatus { get; set; }

    [JsonPropertyName("code")]
    public CodeableConcept? Code { get; set; }

    [JsonPropertyName("subject")]
    public Reference? Subject { get; set; }

    [JsonPropertyName("onsetDateTime")]
    public string? OnsetDateTime { get; set; }

    [JsonPropertyName("abatementDateTime")]
    public string? AbatementDateTime { get; set; }
}

// FHIR MedicationRequest Resource
public class MedicationRequest
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "MedicationRequest";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("intent")]
    public string Intent { get; set; } = string.Empty;

    [JsonPropertyName("medicationCodeableConcept")]
    public CodeableConcept? MedicationCodeableConcept { get; set; }

    [JsonPropertyName("subject")]
    public Reference? Subject { get; set; }

    [JsonPropertyName("authoredOn")]
    public string? AuthoredOn { get; set; }

    [JsonPropertyName("dosageInstruction")]
    public List<DosageInstruction>? DosageInstruction { get; set; }
}

public class DosageInstruction
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("timing")]
    public Timing? Timing { get; set; }

    [JsonPropertyName("route")]
    public CodeableConcept? Route { get; set; }

    [JsonPropertyName("method")]
    public CodeableConcept? Method { get; set; }

    [JsonPropertyName("doseAndRate")]
    public List<DoseAndRate>? DoseAndRate { get; set; }
}

public class DoseAndRate
{
    [JsonPropertyName("type")]
    public CodeableConcept? Type { get; set; }

    [JsonPropertyName("doseQuantity")]
    public Quantity? DoseQuantity { get; set; }

    [JsonPropertyName("doseRange")]
    public Range? DoseRange { get; set; }
}

public class Range
{
    [JsonPropertyName("low")]
    public Quantity? Low { get; set; }

    [JsonPropertyName("high")]
    public Quantity? High { get; set; }
}

public class Timing
{
    [JsonPropertyName("repeat")]
    public TimingRepeat? Repeat { get; set; }

    [JsonPropertyName("code")]
    public CodeableConcept? Code { get; set; }
}

public class TimingRepeat
{
    [JsonPropertyName("frequency")]
    public int? Frequency { get; set; }

    [JsonPropertyName("period")]
    public decimal? Period { get; set; }

    [JsonPropertyName("periodUnit")]
    public string? PeriodUnit { get; set; }

    [JsonPropertyName("when")]
    public List<string>? When { get; set; }
}

// Common FHIR Types
public class CodeableConcept
{
    [JsonPropertyName("coding")]
    public List<Coding>? Coding { get; set; }

    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public class Coding
{
    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }

    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

public class Reference
{
    [JsonPropertyName("reference")]
    public string? ReferenceValue { get; set; }

    [JsonPropertyName("display")]
    public string? Display { get; set; }
}

public class Quantity
{
    [JsonPropertyName("value")]
    public decimal? Value { get; set; }

    [JsonPropertyName("unit")]
    public string? Unit { get; set; }

    [JsonPropertyName("system")]
    public string? System { get; set; }

    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

// FHIR Bundle
public class Bundle<T>
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "Bundle";

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("total")]
    public int? Total { get; set; }

    [JsonPropertyName("entry")]
    public List<BundleEntry<T>>? Entry { get; set; }
}

public class BundleEntry<T>
{
    [JsonPropertyName("fullUrl")]
    public string? FullUrl { get; set; }

    [JsonPropertyName("resource")]
    public T? Resource { get; set; }
}

// FHIR Media Resource (for fundus images)
public class Media
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "Media";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("subject")]
    public Reference? Subject { get; set; }

    [JsonPropertyName("createdDateTime")]
    public string? CreatedDateTime { get; set; }

    [JsonPropertyName("bodySite")]
    public CodeableConcept? BodySite { get; set; }

    [JsonPropertyName("content")]
    public MediaContent? Content { get; set; }

    [JsonPropertyName("note")]
    public List<Annotation>? Note { get; set; }
}

public class MediaContent
{
    [JsonPropertyName("contentType")]
    public string? ContentType { get; set; }

    [JsonPropertyName("data")]
    public string? Data { get; set; }

    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("title")]
    public string? Title { get; set; }
}

public class Annotation
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("time")]
    public string? Time { get; set; }
}

// FHIR DiagnosticReport Resource (for referral recommendations)
public class DiagnosticReport
{
    [JsonPropertyName("resourceType")]
    public string ResourceType { get; set; } = "DiagnosticReport";

    [JsonPropertyName("id")]
    public string Id { get; set; } = string.Empty;

    [JsonPropertyName("status")]
    public string Status { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public List<CodeableConcept>? Category { get; set; }

    [JsonPropertyName("code")]
    public CodeableConcept? Code { get; set; }

    [JsonPropertyName("subject")]
    public Reference? Subject { get; set; }

    [JsonPropertyName("effectiveDateTime")]
    public string? EffectiveDateTime { get; set; }

    [JsonPropertyName("issued")]
    public string? Issued { get; set; }

    [JsonPropertyName("performer")]
    public List<Reference>? Performer { get; set; }

    [JsonPropertyName("media")]
    public List<DiagnosticReportMedia>? Media { get; set; }

    [JsonPropertyName("conclusion")]
    public string? Conclusion { get; set; }

    [JsonPropertyName("conclusionCode")]
    public List<CodeableConcept>? ConclusionCode { get; set; }
}

public class DiagnosticReportMedia
{
    [JsonPropertyName("comment")]
    public string? Comment { get; set; }

    [JsonPropertyName("link")]
    public Reference? Link { get; set; }
}
