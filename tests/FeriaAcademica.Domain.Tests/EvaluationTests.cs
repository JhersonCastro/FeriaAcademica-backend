using FeriaAcademica.Domain.Common;
using FeriaAcademica.Domain.Evaluation;
using FeriaAcademica.Domain.Events;

namespace FeriaAcademica.Domain.Tests;

/// <summary>Executable specification of the evaluation questions of an event and the answers to them.</summary>
public class EvaluationTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private static QuestionDraft Scale(string text = "Clarity", double min = 1, double max = 5, Guid? id = null) => new(id, CriterionType.Scale, text, min, max);
    private static QuestionDraft YesNo(string text = "Recommend?", Guid? id = null) => new(id, CriterionType.YesNo, text, null, null);
    private static QuestionDraft Text(string text = "Improve?", Guid? id = null) => new(id, CriterionType.Text, text, null, null);

    private static Instrument NewInstrument() => Instrument.Create(Guid.NewGuid(), InstrumentType.Survey, EvaluatorGroup.Attendees, "Evaluation");

    private static Instrument WithQuestions(params QuestionDraft[] questions)
    {
        var instrument = NewInstrument();
        instrument.ReplaceQuestions(questions, hasAnswers: false, Now);
        return instrument;
    }

    private static Guid IdOf(Instrument instrument, int order) => instrument.Criteria.Single(c => c.Order == order).Id;

    private static DomainException Fails(Action action) => Assert.Throws<DomainException>(action);

    // ---- the list of questions ----

    [Fact]
    public void The_order_of_the_list_is_the_order_of_the_questions()
    {
        var instrument = WithQuestions(Scale("A"), YesNo("B"), Text("C"));

        Assert.Equal(["A", "B", "C"], instrument.Criteria.OrderBy(c => c.Order).Select(c => c.Text));
        Assert.Equal(Now, instrument.UpdatedAt);
    }

    [Fact]
    public void A_question_with_an_id_is_edited_in_place_and_can_change_place()
    {
        var instrument = WithQuestions(Scale("A"), Scale("B"));
        var (a, b) = (IdOf(instrument, 1), IdOf(instrument, 2));

        var removed = instrument.ReplaceQuestions([Scale("B edited", 0, 10, b), Scale("A", id: a)], hasAnswers: false, Now);

        Assert.Empty(removed);
        Assert.Equal(2, instrument.Criteria.Count);
        var first = instrument.Criteria.Single(c => c.Id == b);
        Assert.Equal((1, "B edited", 0d, 10d), (first.Order, first.Text, first.ScaleMin, first.ScaleMax));
        Assert.Equal(2, instrument.Criteria.Single(c => c.Id == a).Order);
    }

    [Fact]
    public void The_questions_left_out_of_the_list_are_returned_to_be_deleted()
    {
        var instrument = WithQuestions(Scale("A"), Scale("B"), Scale("C"));
        var (a, c) = (IdOf(instrument, 1), IdOf(instrument, 3));

        var removed = instrument.ReplaceQuestions([Scale("A", id: a), Scale("C", id: c)], hasAnswers: false, Now);

        Assert.Equal("B", Assert.Single(removed).Text);
    }

    [Fact]
    public void An_empty_list_removes_every_question()
    {
        var instrument = WithQuestions(Scale("A"), Text("B"));

        var removed = instrument.ReplaceQuestions([], hasAnswers: false, Now);

        Assert.Equal(2, removed.Count);
    }

    [Fact]
    public void Once_somebody_answered_the_questions_cannot_change()
    {
        var instrument = WithQuestions(Scale());

        var ex = Fails(() => instrument.ReplaceQuestions([Scale("Other")], hasAnswers: true, Now));

        Assert.Equal("The evaluation can no longer be changed.", ex.Message);
    }

    [Fact]
    public void An_evaluation_has_at_most_twenty_questions()
    {
        var instrument = NewInstrument();
        var twentyOne = Enumerable.Range(1, Instrument.MaxQuestions + 1).Select(i => Scale($"Q{i}")).ToList();

        Fails(() => instrument.ReplaceQuestions(twentyOne, hasAnswers: false, Now));
        instrument.ReplaceQuestions(twentyOne.Take(Instrument.MaxQuestions).ToList(), hasAnswers: false, Now);

        Assert.Equal(Instrument.MaxQuestions, instrument.Criteria.Count);
    }

    [Fact]
    public void A_question_id_from_another_evaluation_is_rejected()
    {
        var instrument = WithQuestions(Scale());

        Fails(() => instrument.ReplaceQuestions([Scale(id: Guid.NewGuid())], hasAnswers: false, Now));
    }

    [Fact]
    public void A_question_cannot_appear_twice()
    {
        var instrument = WithQuestions(Scale());
        var id = IdOf(instrument, 1);

        Fails(() => instrument.ReplaceQuestions([Scale(id: id), Scale(id: id)], hasAnswers: false, Now));
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void A_question_needs_text(string text) => Fails(() => NewInstrument().ReplaceQuestions([Scale(text)], hasAnswers: false, Now));

    [Fact]
    public void A_question_has_at_most_300_characters()
    {
        var instrument = NewInstrument();

        Fails(() => instrument.ReplaceQuestions([Scale(new string('x', Criterion.MaxTextLength + 1))], hasAnswers: false, Now));
        instrument.ReplaceQuestions([Scale(new string('x', Criterion.MaxTextLength))], hasAnswers: false, Now);
    }

    [Fact]
    public void The_scale_maximum_must_be_greater_than_the_minimum()
    {
        var instrument = NewInstrument();

        Fails(() => instrument.ReplaceQuestions([Scale(min: 3, max: 3)], hasAnswers: false, Now));
        Fails(() => instrument.ReplaceQuestions([Scale(min: 5, max: 1)], hasAnswers: false, Now));
    }

    [Fact]
    public void A_scale_question_needs_its_minimum_and_maximum()
    {
        Fails(() => NewInstrument().ReplaceQuestions([new QuestionDraft(null, CriterionType.Scale, "Clarity", null, null)], hasAnswers: false, Now));
    }

    [Fact]
    public void Yes_no_and_written_questions_ignore_the_scale_they_are_sent()
    {
        var instrument = WithQuestions(new QuestionDraft(null, CriterionType.YesNo, "Recommend?", 3, 9));

        var question = Assert.Single(instrument.Criteria);
        Assert.Equal((0d, 1d), (question.ScaleMin, question.ScaleMax));
    }

    // ---- the answers ----

    private static Response Answer(Instrument instrument, int order, double? value, string? text = null) =>
        Assert.Single(instrument.BuildResponses(Guid.NewGuid(), Guid.NewGuid(), [new AnswerDraft(IdOf(instrument, order), value, text)], Now));

    [Fact]
    public void A_scale_answer_inside_the_scale_is_accepted()
    {
        var instrument = WithQuestions(Scale(min: 1, max: 5));

        Assert.Equal(5, Answer(instrument, 1, 5).Value);
        Assert.Equal(1, Answer(instrument, 1, 1).Value);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(6)]
    [InlineData(2.5)]
    public void A_scale_answer_outside_the_scale_or_not_whole_is_rejected(double value)
    {
        var instrument = WithQuestions(Scale(min: 1, max: 5));

        Fails(() => Answer(instrument, 1, value));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    public void A_yes_no_answer_is_zero_or_one(double value) => Assert.Equal(value, Answer(WithQuestions(YesNo()), 1, value).Value);

    [Fact]
    public void A_yes_no_answer_cannot_be_anything_else()
    {
        var instrument = WithQuestions(YesNo());

        Fails(() => Answer(instrument, 1, 2));
        Fails(() => Answer(instrument, 1, 0.5));
    }

    [Fact]
    public void A_written_answer_keeps_its_trimmed_text_and_no_number()
    {
        var response = Answer(WithQuestions(Text()), 1, null, "  More examples  ");

        Assert.Equal("More examples", response.Comment);
        Assert.Null(response.Value);
    }

    [Fact]
    public void A_written_answer_is_text_only_and_not_blank_and_at_most_1000_characters()
    {
        var instrument = WithQuestions(Text());

        Fails(() => Answer(instrument, 1, 3, "text"));
        Fails(() => Answer(instrument, 1, null, "   "));
        Fails(() => Answer(instrument, 1, null, new string('x', Criterion.MaxAnswerLength + 1)));
        Answer(instrument, 1, null, new string('x', Criterion.MaxAnswerLength));
    }

    [Fact]
    public void A_number_question_is_answered_with_a_number_and_no_text()
    {
        var instrument = WithQuestions(Scale());

        Fails(() => Answer(instrument, 1, null));
        Fails(() => Answer(instrument, 1, 3, "text"));
    }

    [Fact]
    public void An_answer_needs_a_question_of_this_evaluation_and_each_question_is_answered_once()
    {
        var instrument = WithQuestions(Scale());
        var id = IdOf(instrument, 1);

        Fails(() => instrument.BuildResponses(Guid.NewGuid(), Guid.NewGuid(), [new AnswerDraft(Guid.NewGuid(), 3, null)], Now));
        Fails(() => instrument.BuildResponses(Guid.NewGuid(), Guid.NewGuid(), [new AnswerDraft(id, 3, null), new AnswerDraft(id, 4, null)], Now));
    }

    [Fact]
    public void Sending_no_answers_at_all_is_rejected()
    {
        Fails(() => WithQuestions(Scale()).BuildResponses(Guid.NewGuid(), Guid.NewGuid(), [], Now));
    }

    // ---- who can do what ----

    [Fact]
    public void Only_the_super_admin_coordinators_and_teachers_manage_the_evaluation()
    {
        EvaluationPolicy.EnsureCanManage(true, []);
        EvaluationPolicy.EnsureCanManage(false, [EventRole.Coordinator]);
        EvaluationPolicy.EnsureCanManage(false, [EventRole.Teacher]);

        Fails(() => EvaluationPolicy.EnsureCanManage(false, [EventRole.Jury, EventRole.Participant, EventRole.Attendee]));
        Fails(() => EvaluationPolicy.EnsureCanManage(false, []));
    }
}
