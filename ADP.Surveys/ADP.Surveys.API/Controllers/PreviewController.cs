using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftSoftware.ADP.Surveys.Data.Bank;
using ShiftSoftware.ADP.Surveys.Shared.DTOs;
using ShiftSoftware.ADP.Surveys.Shared.Json;
using ShiftSoftware.ADP.Surveys.Shared.Resolver;

namespace ShiftSoftware.ADP.Surveys.API.Controllers;

/// <summary>
/// Preview-only resolve endpoint for the builder's live-preview pane. Takes a
/// draft <see cref="SurveyDto"/> (with <c>bankRef</c> / <c>templateRef</c>
/// entries still in ref form) and returns the fully-resolved schema — the same
/// shape <see cref="PublicSurveyController.GetSchema"/> serves to the renderer.
///
/// Unlike <see cref="PublishController"/>, this endpoint does NOT persist, hash,
/// lock bank entries, or run the integrity validator. It's a pure resolve step
/// so the builder can show the author a WYSIWYG preview that matches production.
///
/// Auth: <see cref="Authorize"/> — only signed-in builders see previews (same
/// audience as the CRUD pages).
/// </summary>
[Route("[controller]")]
[ApiController]
[Authorize]
public class PreviewController : ControllerBase
{
    private readonly EfBankSource bankSource;

    public PreviewController(EfBankSource bankSource)
    {
        this.bankSource = bankSource;
    }

    /// <summary>
    /// Resolves a draft into inline form. Errors come back with dotted paths so
    /// the builder can highlight the offending ref exactly like Publish does.
    /// Reads the body as a raw stream and deserializes with
    /// <see cref="SurveySchemaSerializer.Options"/> directly so the schema's
    /// custom converters + polymorphism kick in — the default ASP.NET Core
    /// JSON settings don't match our canonical wire format and would fail on
    /// the polymorphic <c>QuestionEntryDto</c> / <c>ScreenDto</c> shapes.
    /// Response is also serialized via the canonical options.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Resolve()
    {
        SurveyDto? draft;
        try
        {
            draft = await JsonSerializer.DeserializeAsync<SurveyDto>(
                Request.Body,
                SurveySchemaSerializer.Options,
                HttpContext.RequestAborted);
        }
        catch (JsonException ex)
        {
            return BadRequest(new { Message = $"Invalid draft JSON: {ex.Message}" });
        }
        if (draft is null) return BadRequest(new { Message = "Missing body." });

        var result = SchemaResolver.Resolve(draft, bankSource);
        if (!result.Success)
        {
            return BadRequest(new
            {
                Message = "Draft has unresolved bank or template references.",
                Errors = result.Errors.Select(e => new { e.Path, e.Message }),
            });
        }

        // Hand-serialize so the canonical settings apply both ways — no PascalCase
        // leak, no duplicate `QuestionType` fields, polymorphism preserved.
        var json = JsonSerializer.Serialize(result.Survey, SurveySchemaSerializer.Options);
        return Content(json, "application/json");
    }
}
