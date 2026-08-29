namespace InternshipManagementSystem.Assessment;

/// <summary>
/// Question types are identifiers, not a closed enum: a new type is a new
/// <c>IQuestionGrader</c> registration plus a payload shape, with no schema
/// change and no edit to any existing grader (UC-16).
/// <para>
/// These constants are the types the platform ships with. A tenant-specific
/// or later-added type is just another string; anything without a registered
/// grader falls through to manual review.
/// </para>
/// </summary>
public static class QuestionTypes
{
    /// <summary>Free text. Graded by a human, optionally against a rubric.</summary>
    public const string Text = "text";

    /// <summary>One correct option out of several.</summary>
    public const string SingleChoice = "single-choice";

    /// <summary>Several correct options. Scoring honours <c>allowPartialCredit</c>.</summary>
    public const string MultiSelect = "multi-select";

    /// <summary>True or false.</summary>
    public const string TrueFalse = "true-false";

    /// <summary>Pair each left item with its right item. Vocabulary, terminology, definitions.</summary>
    public const string Matching = "matching";

    /// <summary>Put the items in the correct order. Procedures, workflows, safety steps.</summary>
    public const string Ordering = "ordering";

    /// <summary>A number accepted within a tolerance. Maths, finance, position sizing.</summary>
    public const string Numeric = "numeric";

    /// <summary>Click the right region of an image. Charts, anatomy, diagrams, maps.</summary>
    public const string Hotspot = "hotspot";

    /// <summary>Fill the blanks in a sentence.</summary>
    public const string FillInTheBlank = "fill-in-the-blank";

    /// <summary>Code whose expected output is compared as text. Not executed — see the constraint in requirements.md.</summary>
    public const string Code = "code";

    /// <summary>Upload a document, image, audio or video as the answer. Human graded.</summary>
    public const string FileUpload = "file-upload";

    /// <summary>Record a spoken answer. Language proficiency. Human graded.</summary>
    public const string AudioResponse = "audio-response";

    /// <summary>A point on an agree/disagree scale. Soft skills, behavioural surveys.</summary>
    public const string Scale = "scale";
}
