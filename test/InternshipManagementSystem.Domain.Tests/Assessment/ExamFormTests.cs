using System;
using InternshipManagementSystem.Assessment.Exams;
using Shouldly;
using Volo.Abp;
using Xunit;

namespace InternshipManagementSystem.Assessment.Exams;

/// <summary>
/// A named form, and the two things publishing one must refuse.
/// <para>
/// Publishing is where a form stops being an author's draft and becomes the
/// paper a room of people will sit. Everything checked here is something that
/// cannot be fixed afterwards without invalidating results already earned.
/// </para>
/// </summary>
public class ExamFormTests
{
    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-000000000009");
    private static readonly Guid ExamId = Guid.Parse("dddddddd-0000-0000-0000-000000000001");

    private static ExamForm Form() =>
        new(Guid.NewGuid(), Tenant, ExamId, "Form 1", "F1");

    private static ExamFormQuestion Slot(ExamForm form, Guid questionId, int order, decimal score = 1m) =>
        new(Guid.NewGuid(), Tenant, form.Id, questionId, order, score);

    [Fact]
    public void An_empty_form_cannot_be_published()
    {
        var form = Form();

        var thrown = Should.Throw<BusinessException>(() => form.Publish(0m));

        thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamFormHasNoQuestions);
        form.Status.ShouldBe(ExamFormStatus.Draft);
    }

    [Fact]
    public void A_form_carrying_the_same_question_twice_cannot_be_published()
    {
        var form = Form();
        var repeated = Guid.NewGuid();

        form.Questions.Add(Slot(form, repeated, 0));
        form.Questions.Add(Slot(form, repeated, 1));

        // Scored twice and read twice, and the taker reasonably concludes the exam
        // is broken. Caught here as well as by a unique index, because the message
        // an author gets should name the problem rather than the constraint.
        var thrown = Should.Throw<BusinessException>(() => form.Publish(2m));

        thrown.Code.ShouldBe(InternshipManagementSystemDomainErrorCodes.ExamFormHasDuplicateQuestions);
    }

    [Fact]
    public void Publishing_freezes_the_total_marks()
    {
        var form = Form();
        form.Questions.Add(Slot(form, Guid.NewGuid(), 0, 3m));
        form.Questions.Add(Slot(form, Guid.NewGuid(), 1, 2m));

        form.Publish(5m);

        // Frozen rather than summed on demand: a question's marks can be edited
        // afterwards, and a result must keep meaning what it meant on the day it
        // was earned.
        form.MaxScore.ShouldBe(5m);
        form.Status.ShouldBe(ExamFormStatus.Published);
        form.IsUsable.ShouldBeTrue();
    }

    [Fact]
    public void A_retired_form_stops_being_offered_but_does_not_disappear()
    {
        var form = Form();
        form.Questions.Add(Slot(form, Guid.NewGuid(), 0, 1m));
        form.Publish(1m);

        form.Retire();

        // Results reference it and an equating study still has to read it, so
        // retiring is not deleting.
        form.IsUsable.ShouldBeFalse();
        form.Status.ShouldBe(ExamFormStatus.Retired);
        form.Questions.Count.ShouldBe(1);
    }
}

/// <summary>
/// A section's own pass mark, which is the part that changes an outcome.
/// </summary>
public class ExamSectionTests
{
    private static ExamSection Section(decimal? minimum) =>
        new(Guid.NewGuid(), null, Guid.NewGuid(), "Risk management")
        {
            MinimumPercentage = minimum,
        };

    [Fact]
    public void A_section_below_its_own_minimum_fails_however_well_the_rest_went()
    {
        var section = Section(50m);

        // Passing overall while failing risk management is not a pass anyone
        // should have to defend.
        section.IsFailedAt(49m).ShouldBeTrue();
        section.IsFailedAt(50m).ShouldBeFalse();
        section.IsFailedAt(80m).ShouldBeFalse();
    }

    [Fact]
    public void A_section_without_a_minimum_only_contributes_to_the_total()
    {
        var section = Section(null);

        section.IsFailedAt(0m).ShouldBeFalse();
    }
}
