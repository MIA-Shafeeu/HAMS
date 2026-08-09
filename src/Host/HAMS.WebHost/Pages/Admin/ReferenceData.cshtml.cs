using HAMS.AssessmentEvaluation.Application;
using HAMS.Attendance.Application;
using HAMS.Intervention.Application;
using HAMS.LearningDelivery.Application;
using HAMS.OrgCurriculum.Application;
using HAMS.PeopleEnrollment.Application;
using HAMS.Platform.Access;
using HAMS.WebHost.Components.Account;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace HAMS.WebHost.Pages.Admin;

public enum LookupExtraFieldKind { None, Rank, IsPositive }

public sealed record LookupRow(Guid Id, string Code, string Name, int DisplayOrder, bool IsActive, int? Rank = null, bool? IsPositive = null);

public sealed record LookupSection(string Key, string Title, IReadOnlyList<LookupRow> Items, LookupExtraFieldKind ExtraField);

public sealed record LookupGroup(string Title, IReadOnlyList<LookupSection> Sections);

[Authorize(Policy = SystemOrSchoolAdminPolicy.Name)]
public sealed class ReferenceDataModel(
    IPersonRoleAssignmentService roleAssignments,
    IOrgAdminService orgAdmin,
    ICurriculumAdminService curriculumAdmin,
    IPeopleAdminService peopleAdmin,
    IAttendanceAdminService attendanceAdmin,
    IInterventionAdminService interventionAdmin,
    IAssessmentConfigAdminService assessmentConfig,
    ILessonPlanningService lessonPlanning) : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string Tab { get; set; } = "roles";

    [BindProperty(SupportsGet = true)]
    public string? EditLookupKey { get; set; }

    [BindProperty(SupportsGet = true)]
    public Guid? EditLookupId { get; set; }

    public IReadOnlyList<LookupGroup> Groups { get; private set; } = [];

    [BindProperty] public string Code { get; set; } = "";
    [BindProperty] public string Name { get; set; } = "";
    [BindProperty] public int DisplayOrder { get; set; }
    [BindProperty] public int Rank { get; set; }
    [BindProperty] public bool IsPositive { get; set; }

    public async Task OnGetAsync()
    {
        var roles = (await roleAssignments.GetAllRolesAsync()).Select(r => new LookupRow(r.Id, r.Code, r.Name, r.DisplayOrder, r.IsActive)).ToList();
        var tiers = (await roleAssignments.GetConfidentialityTiersAsync()).Select(t => new LookupRow(t.Id, t.Code, t.Name, t.DisplayOrder, t.IsActive, Rank: t.Rank)).ToList();

        var evalModels = (await orgAdmin.GetEvaluationModelsAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var deliveryModes = (await curriculumAdmin.GetAllDeliveryModesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var mediums = (await curriculumAdmin.GetAllMediumsOfInstructionAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var holidayTypes = (await orgAdmin.GetHolidayTypesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();

        var relationshipTypes = (await peopleAdmin.GetAllRelationshipTypesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var restrictionTypes = (await peopleAdmin.GetAllRestrictionTypesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var employmentStatuses = (await peopleAdmin.GetAllEmploymentStatusesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var enrollmentTypes = (await peopleAdmin.GetAllEnrollmentTypesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();

        var attendanceStatuses = (await attendanceAdmin.GetAttendanceStatusesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var behaviourCategories = (await interventionAdmin.GetBehaviourCategoriesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive, IsPositive: x.IsPositive)).ToList();
        var interventionTypes = (await interventionAdmin.GetInterventionTypesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();

        var assessmentCategories = (await assessmentConfig.GetAssessmentCategoriesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var examBoards = (await assessmentConfig.GetExternalExaminationBoardsAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var specialResultStates = (await assessmentConfig.GetSpecialResultStatesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var aggregationRules = (await assessmentConfig.GetResultAggregationRulesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();

        var resourceTypes = (await lessonPlanning.GetResourceTypesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();
        var evidenceTypes = (await lessonPlanning.GetEvidenceTypesAsync()).Select(x => new LookupRow(x.Id, x.Code, x.Name, x.DisplayOrder, x.IsActive)).ToList();

        Groups =
        [
            new("Roles & Confidentiality",
            [
                new("role", "Roles", roles, LookupExtraFieldKind.None),
                new("confidentialityTier", "Confidentiality Tiers", tiers, LookupExtraFieldKind.Rank),
            ]),
            new("Curriculum",
            [
                new("evaluationModel", "Evaluation Models", evalModels, LookupExtraFieldKind.None),
                new("deliveryMode", "Delivery Modes", deliveryModes, LookupExtraFieldKind.None),
                new("mediumOfInstruction", "Mediums of Instruction", mediums, LookupExtraFieldKind.None),
                new("holidayType", "Holiday Types", holidayTypes, LookupExtraFieldKind.None),
            ]),
            new("People",
            [
                new("relationshipType", "Guardian Relationship Types", relationshipTypes, LookupExtraFieldKind.None),
                new("restrictionType", "Restriction Types", restrictionTypes, LookupExtraFieldKind.None),
                new("employmentStatus", "Employment Statuses", employmentStatuses, LookupExtraFieldKind.None),
                new("enrollmentType", "Enrollment Types", enrollmentTypes, LookupExtraFieldKind.None),
            ]),
            new("Attendance & Behaviour",
            [
                new("attendanceStatus", "Attendance Statuses", attendanceStatuses, LookupExtraFieldKind.None),
                new("behaviourCategory", "Behaviour Categories", behaviourCategories, LookupExtraFieldKind.IsPositive),
                new("interventionType", "Intervention Types", interventionTypes, LookupExtraFieldKind.None),
            ]),
            new("Assessment",
            [
                new("assessmentCategory", "Assessment Categories", assessmentCategories, LookupExtraFieldKind.None),
                new("examBoard", "External Examination Boards", examBoards, LookupExtraFieldKind.None),
                new("specialResultState", "Special Result States", specialResultStates, LookupExtraFieldKind.None),
                new("aggregationRule", "Result Aggregation Rules", aggregationRules, LookupExtraFieldKind.None),
            ]),
            new("Learning Resources",
            [
                new("resourceType", "Resource Types", resourceTypes, LookupExtraFieldKind.None),
                new("evidenceType", "Evidence Types", evidenceTypes, LookupExtraFieldKind.None),
            ]),
        ];
    }

    public async Task<IActionResult> OnPostCreateAsync(string lookup)
    {
        switch (lookup)
        {
            case "role": await roleAssignments.CreateRoleAsync(Code, Name, null, DisplayOrder); break;
            case "confidentialityTier": await roleAssignments.CreateConfidentialityTierAsync(Code, Name, null, Rank, DisplayOrder); break;
            case "evaluationModel": await orgAdmin.CreateEvaluationModelAsync(Code, Name, null, DisplayOrder); break;
            case "deliveryMode": await curriculumAdmin.CreateDeliveryModeAsync(Code, Name, DisplayOrder); break;
            case "mediumOfInstruction": await curriculumAdmin.CreateMediumOfInstructionAsync(Code, Name, DisplayOrder); break;
            case "holidayType": await orgAdmin.CreateHolidayTypeAsync(Code, Name, DisplayOrder); break;
            case "relationshipType": await peopleAdmin.CreateRelationshipTypeAsync(Code, Name, DisplayOrder); break;
            case "restrictionType": await peopleAdmin.CreateRestrictionTypeAsync(Code, Name, DisplayOrder); break;
            case "employmentStatus": await peopleAdmin.CreateEmploymentStatusAsync(Code, Name, DisplayOrder); break;
            case "enrollmentType": await peopleAdmin.CreateEnrollmentTypeAsync(Code, Name, DisplayOrder); break;
            case "attendanceStatus": await attendanceAdmin.CreateAttendanceStatusAsync(Code, Name, DisplayOrder); break;
            case "behaviourCategory": await interventionAdmin.CreateBehaviourCategoryAsync(Code, Name, IsPositive, DisplayOrder); break;
            case "interventionType": await interventionAdmin.CreateInterventionTypeAsync(Code, Name, DisplayOrder); break;
            case "assessmentCategory": await assessmentConfig.CreateAssessmentCategoryAsync(Code, Name, DisplayOrder); break;
            case "examBoard": await assessmentConfig.CreateExternalExaminationBoardAsync(Code, Name, DisplayOrder); break;
            case "specialResultState": await assessmentConfig.CreateSpecialResultStateAsync(Code, Name, DisplayOrder); break;
            case "aggregationRule": await assessmentConfig.CreateResultAggregationRuleAsync(Code, Name, DisplayOrder); break;
            case "resourceType": await lessonPlanning.CreateResourceTypeAsync(Code, Name, DisplayOrder); break;
            case "evidenceType": await lessonPlanning.CreateEvidenceTypeAsync(Code, Name, DisplayOrder); break;
        }

        TempData["FlashMessage"] = "Created.";
        TempData["FlashSeverity"] = "success";
        return RedirectToPage(new { Tab });
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(string lookup, Guid id, bool isActive)
    {
        switch (lookup)
        {
            case "role": await roleAssignments.SetRoleActiveAsync(id, isActive); break;
            case "confidentialityTier": await roleAssignments.SetConfidentialityTierActiveAsync(id, isActive); break;
            case "evaluationModel": await orgAdmin.SetEvaluationModelActiveAsync(id, isActive); break;
            case "deliveryMode": await curriculumAdmin.SetDeliveryModeActiveAsync(id, isActive); break;
            case "mediumOfInstruction": await curriculumAdmin.SetMediumOfInstructionActiveAsync(id, isActive); break;
            case "holidayType": await orgAdmin.SetHolidayTypeActiveAsync(id, isActive); break;
            case "relationshipType": await peopleAdmin.SetRelationshipTypeActiveAsync(id, isActive); break;
            case "restrictionType": await peopleAdmin.SetRestrictionTypeActiveAsync(id, isActive); break;
            case "employmentStatus": await peopleAdmin.SetEmploymentStatusActiveAsync(id, isActive); break;
            case "enrollmentType": await peopleAdmin.SetEnrollmentTypeActiveAsync(id, isActive); break;
            case "attendanceStatus": await attendanceAdmin.SetAttendanceStatusActiveAsync(id, isActive); break;
            case "behaviourCategory": await interventionAdmin.SetBehaviourCategoryActiveAsync(id, isActive); break;
            case "interventionType": await interventionAdmin.SetInterventionTypeActiveAsync(id, isActive); break;
            case "assessmentCategory": await assessmentConfig.SetAssessmentCategoryActiveAsync(id, isActive); break;
            case "examBoard": await assessmentConfig.SetExternalExaminationBoardActiveAsync(id, isActive); break;
            case "specialResultState": await assessmentConfig.SetSpecialResultStateActiveAsync(id, isActive); break;
            case "aggregationRule": await assessmentConfig.SetResultAggregationRuleActiveAsync(id, isActive); break;
            case "resourceType": await lessonPlanning.SetResourceTypeActiveAsync(id, isActive); break;
            case "evidenceType": await lessonPlanning.SetEvidenceTypeActiveAsync(id, isActive); break;
        }

        return RedirectToPage(new { Tab });
    }

    public async Task<IActionResult> OnPostUpdateAsync(string lookup, Guid id)
    {
        switch (lookup)
        {
            case "role": await roleAssignments.UpdateRoleAsync(id, Name, DisplayOrder); break;
            case "confidentialityTier": await roleAssignments.UpdateConfidentialityTierAsync(id, Name, Rank, DisplayOrder); break;
            case "evaluationModel": await orgAdmin.UpdateEvaluationModelAsync(id, Name, DisplayOrder); break;
            case "deliveryMode": await curriculumAdmin.UpdateDeliveryModeAsync(id, Name, DisplayOrder); break;
            case "mediumOfInstruction": await curriculumAdmin.UpdateMediumOfInstructionAsync(id, Name, DisplayOrder); break;
            case "holidayType": await orgAdmin.UpdateHolidayTypeAsync(id, Name, DisplayOrder); break;
            case "relationshipType": await peopleAdmin.UpdateRelationshipTypeAsync(id, Name, DisplayOrder); break;
            case "restrictionType": await peopleAdmin.UpdateRestrictionTypeAsync(id, Name, DisplayOrder); break;
            case "employmentStatus": await peopleAdmin.UpdateEmploymentStatusAsync(id, Name, DisplayOrder); break;
            case "enrollmentType": await peopleAdmin.UpdateEnrollmentTypeAsync(id, Name, DisplayOrder); break;
            case "attendanceStatus": await attendanceAdmin.UpdateAttendanceStatusAsync(id, Name, DisplayOrder); break;
            case "behaviourCategory": await interventionAdmin.UpdateBehaviourCategoryAsync(id, Name, IsPositive, DisplayOrder); break;
            case "interventionType": await interventionAdmin.UpdateInterventionTypeAsync(id, Name, DisplayOrder); break;
            case "assessmentCategory": await assessmentConfig.UpdateAssessmentCategoryAsync(id, Name, DisplayOrder); break;
            case "examBoard": await assessmentConfig.UpdateExternalExaminationBoardAsync(id, Name, DisplayOrder); break;
            case "specialResultState": await assessmentConfig.UpdateSpecialResultStateAsync(id, Name, DisplayOrder); break;
            case "aggregationRule": await assessmentConfig.UpdateResultAggregationRuleAsync(id, Name, DisplayOrder); break;
            case "resourceType": await lessonPlanning.UpdateResourceTypeAsync(id, Name, DisplayOrder); break;
            case "evidenceType": await lessonPlanning.UpdateEvidenceTypeAsync(id, Name, DisplayOrder); break;
        }

        TempData["FlashMessage"] = "Updated.";
        TempData["FlashSeverity"] = "success";
        return RedirectToPage(new { Tab });
    }
}
