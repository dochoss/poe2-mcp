using FluentAssertions;
using Poe2Mcp.Core.AI;
using Xunit;

namespace Poe2Mcp.Tests.AI;

public class MechanicsKnowledgeBaseTests
{
    private readonly IMechanicsKnowledgeBase _knowledgeBase;

    public MechanicsKnowledgeBaseTests()
    {
        _knowledgeBase = new PoE2MechanicsKnowledgeBase();
    }

    [Fact]
    public void Constructor_InitializesMechanics()
    {
        // Act
        var allMechanics = _knowledgeBase.GetAllMechanics();

        // Assert
        allMechanics.Should().NotBeNull();
        allMechanics.Should().NotBeEmpty();
        allMechanics.Should().HaveCountGreaterThanOrEqualTo(6); // We have 6 mechanics defined
    }

    [Theory]
    [InlineData("freeze")]
    [InlineData("shock")]
    [InlineData("stun")]
    [InlineData("crit")]
    [InlineData("spirit")]
    [InlineData("increased_vs_more")]
    public void GetMechanic_WithValidName_ReturnsMechanic(string mechanicName)
    {
        // Act
        var mechanic = _knowledgeBase.GetMechanic(mechanicName);

        // Assert
        mechanic.Should().NotBeNull();
        mechanic!.Name.Should().NotBeNullOrEmpty();
        mechanic.ShortDescription.Should().NotBeNullOrEmpty();
        mechanic.DetailedExplanation.Should().NotBeNullOrEmpty();
        mechanic.HowItWorks.Should().NotBeNullOrEmpty();
    }

    [Theory]
    [InlineData("FREEZE")]
    [InlineData("Freeze")]
    [InlineData("fReEzE")]
    public void GetMechanic_IsCaseInsensitive(string mechanicName)
    {
        // Act
        var mechanic = _knowledgeBase.GetMechanic(mechanicName);

        // Assert
        mechanic.Should().NotBeNull();
        mechanic!.Name.Should().Be("Freeze");
    }

    [Fact]
    public void GetMechanic_WithInvalidName_ReturnsNull()
    {
        // Act
        var mechanic = _knowledgeBase.GetMechanic("nonexistent_mechanic");

        // Assert
        mechanic.Should().BeNull();
    }

    [Fact]
    public void GetMechanic_Freeze_HasExpectedProperties()
    {
        // Act
        var freeze = _knowledgeBase.GetMechanic("freeze");

        // Assert
        freeze.Should().NotBeNull();
        freeze!.Name.Should().Be("Freeze");
        freeze.Category.Should().Be(MechanicCategory.Ailments);
        freeze.CalculationFormula.Should().NotBeNullOrEmpty();
        freeze.Examples.Should().NotBeEmpty();
        freeze.CommonQuestions.Should().NotBeEmpty();
        freeze.RelatedMechanics.Should().NotBeEmpty();
        freeze.ImportantNotes.Should().NotBeEmpty();
        freeze.ChangedFromPoe1.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void GetMechanic_Shock_HasExpectedProperties()
    {
        // Act
        var shock = _knowledgeBase.GetMechanic("shock");

        // Assert
        shock.Should().NotBeNull();
        shock!.Name.Should().Be("Shock");
        shock.Category.Should().Be(MechanicCategory.Ailments);
        shock.CalculationFormula.Should().Contain("50%"); // Max shock magnitude
        shock.CommonQuestions.Should().ContainKey("Does shock affect my allies' damage?");
    }

    [Fact]
    public void GetMechanic_Stun_HasExpectedProperties()
    {
        // Act
        var stun = _knowledgeBase.GetMechanic("stun");

        // Assert
        stun.Should().NotBeNull();
        stun!.Name.Should().Be("Stun");
        stun.Category.Should().Be(MechanicCategory.CrowdControl);
        stun.HowItWorks.Should().Contain("Light Stun");
        stun.HowItWorks.Should().Contain("Heavy Stun");
    }

    [Fact]
    public void GetMechanic_Crit_HasExpectedProperties()
    {
        // Act
        var crit = _knowledgeBase.GetMechanic("crit");

        // Assert
        crit.Should().NotBeNull();
        crit!.Name.Should().Be("Critical Strike");
        crit.Category.Should().Be(MechanicCategory.Damage);
        crit.CalculationFormula.Should().Contain("200%"); // Base crit in PoE2
    }

    [Fact]
    public void GetMechanic_Spirit_HasExpectedProperties()
    {
        // Act
        var spirit = _knowledgeBase.GetMechanic("spirit");

        // Assert
        spirit.Should().NotBeNull();
        spirit!.Name.Should().Be("Spirit");
        spirit.Category.Should().Be(MechanicCategory.Resources);
        spirit.ChangedFromPoe1.Should().Contain("new in PoE2");
    }

    [Fact]
    public void GetMechanic_IncreasedVsMore_HasExpectedProperties()
    {
        // Act
        var mechanic = _knowledgeBase.GetMechanic("increased_vs_more");

        // Assert
        mechanic.Should().NotBeNull();
        mechanic!.Name.Should().Be("Increased vs More Damage");
        mechanic.Category.Should().Be(MechanicCategory.Scaling);
        mechanic.CalculationFormula.Should().Contain("ΣIncreased");
        mechanic.ShortDescription.Should().Contain("additive vs multiplicative");
    }

    [Theory]
    [InlineData("damage", 2)] // Should find "increased_vs_more" and "crit"
    [InlineData("freeze", 1)]
    [InlineData("cold", 1)]
    [InlineData("lightning", 1)]
    public void SearchMechanics_FindsRelevantMechanics(string query, int minExpectedResults)
    {
        // Act
        var results = _knowledgeBase.SearchMechanics(query);

        // Assert
        results.Should().NotBeNull();
        results.Should().HaveCountGreaterThanOrEqualTo(minExpectedResults);
    }

    [Fact]
    public void SearchMechanics_IsCaseInsensitive()
    {
        // Act
        var lowerResults = _knowledgeBase.SearchMechanics("freeze");
        var upperResults = _knowledgeBase.SearchMechanics("FREEZE");
        var mixedResults = _knowledgeBase.SearchMechanics("FrEeZe");

        // Assert
        lowerResults.Should().HaveCount(upperResults.Count);
        lowerResults.Should().HaveCount(mixedResults.Count);
    }

    [Fact]
    public void SearchMechanics_WithNoMatches_ReturnsEmptyList()
    {
        // Act
        var results = _knowledgeBase.SearchMechanics("xyzabc123nonexistent");

        // Assert
        results.Should().NotBeNull();
        results.Should().BeEmpty();
    }

    [Theory]
    [InlineData(MechanicCategory.Ailments, 2)] // freeze, shock
    [InlineData(MechanicCategory.CrowdControl, 1)] // stun
    [InlineData(MechanicCategory.Damage, 1)] // crit
    [InlineData(MechanicCategory.Resources, 1)] // spirit
    [InlineData(MechanicCategory.Scaling, 1)] // increased_vs_more
    public void GetByCategory_ReturnsCorrectCount(MechanicCategory category, int expectedCount)
    {
        // Act
        var mechanics = _knowledgeBase.GetByCategory(category);

        // Assert
        mechanics.Should().NotBeNull();
        mechanics.Should().HaveCount(expectedCount);
        mechanics.Should().AllSatisfy(m => m.Category.Should().Be(category));
    }

    [Fact]
    public void GetByCategory_Defense_ReturnsEmpty()
    {
        // Act
        var mechanics = _knowledgeBase.GetByCategory(MechanicCategory.Defense);

        // Assert
        mechanics.Should().NotBeNull();
        mechanics.Should().BeEmpty();
    }

    [Fact]
    public void FormatMechanicExplanation_WithFullDetails_ContainsAllSections()
    {
        // Arrange
        var freeze = _knowledgeBase.GetMechanic("freeze")!;

        // Act
        var formatted = _knowledgeBase.FormatMechanicExplanation(freeze, includeAll: true);

        // Assert
        formatted.Should().Contain("FREEZE");
        formatted.Should().Contain("Category:");
        formatted.Should().Contain("DETAILED EXPLANATION");
        formatted.Should().Contain("HOW IT WORKS");
        formatted.Should().Contain("CALCULATION");
        formatted.Should().Contain("EXAMPLES");
        formatted.Should().Contain("COMMON QUESTIONS");
        formatted.Should().Contain("IMPORTANT NOTES");
        formatted.Should().Contain("CHANGES FROM POE1");
        formatted.Should().Contain("RELATED:");
    }

    [Fact]
    public void FormatMechanicExplanation_WithoutFullDetails_ContainsBasicSections()
    {
        // Arrange
        var freeze = _knowledgeBase.GetMechanic("freeze")!;

        // Act
        var formatted = _knowledgeBase.FormatMechanicExplanation(freeze, includeAll: false);

        // Assert
        formatted.Should().Contain("FREEZE");
        formatted.Should().Contain("DETAILED EXPLANATION");
        formatted.Should().Contain("HOW IT WORKS");
        formatted.Should().NotContain("EXAMPLES");
        formatted.Should().NotContain("COMMON QUESTIONS");
    }

    [Fact]
    public void AnswerQuestion_WithMatchingQuestion_ReturnsAnswer()
    {
        // Act
        var answer = _knowledgeBase.AnswerQuestion("Does freeze work on bosses?");

        // Assert
        answer.Should().NotBeNullOrEmpty();
        answer.Should().Contain("Freeze");
        answer.Should().Contain("bosses");
    }

    [Fact]
    public void AnswerQuestion_WithPartialMatch_ReturnsAnswer()
    {
        // Act
        var answer = _knowledgeBase.AnswerQuestion("shock bosses");

        // Assert
        answer.Should().NotBeNullOrEmpty();
        answer.Should().Contain("Shock");
    }

    [Fact]
    public void AnswerQuestion_WithNoMatch_ReturnsNull()
    {
        // Act
        var answer = _knowledgeBase.AnswerQuestion("How do I bake a cake?");

        // Assert
        answer.Should().BeNull();
    }

    [Fact]
    public void AnswerQuestion_IsCaseInsensitive()
    {
        // Act
        var lowerAnswer = _knowledgeBase.AnswerQuestion("does freeze work on bosses?");
        var upperAnswer = _knowledgeBase.AnswerQuestion("DOES FREEZE WORK ON BOSSES?");

        // Assert
        lowerAnswer.Should().NotBeNull();
        upperAnswer.Should().NotBeNull();
        lowerAnswer.Should().Contain("Freeze");
        upperAnswer.Should().Contain("Freeze");
    }

    [Fact]
    public void GetAllMechanics_ReturnsAllMechanics()
    {
        // Act
        var allMechanics = _knowledgeBase.GetAllMechanics();

        // Assert
        allMechanics.Should().NotBeNull();
        allMechanics.Should().ContainKey("freeze");
        allMechanics.Should().ContainKey("shock");
        allMechanics.Should().ContainKey("stun");
        allMechanics.Should().ContainKey("crit");
        allMechanics.Should().ContainKey("spirit");
        allMechanics.Should().ContainKey("increased_vs_more");
    }

    [Fact]
    public void MechanicExplanation_IsImmutable()
    {
        // Arrange
        var mechanic = _knowledgeBase.GetMechanic("freeze")!;
        var originalExamples = mechanic.Examples;
        var originalQuestions = mechanic.CommonQuestions;

        // Act & Assert - Records are immutable, these should be new instances
        mechanic.Should().NotBeNull();
        originalExamples.Should().BeSameAs(mechanic.Examples);
        originalQuestions.Should().BeSameAs(mechanic.CommonQuestions);
    }

    [Theory]
    [InlineData("freeze", "Does freeze work on bosses?")]
    [InlineData("shock", "Does shock affect my allies' damage?")]
    [InlineData("stun", "Can I stun bosses?")]
    [InlineData("crit", "Is crit better than non-crit?")]
    [InlineData("spirit", "How do I get more Spirit?")]
    [InlineData("increased_vs_more", "Is 'more' always better than 'increased'?")]
    public void Mechanics_HaveCommonQuestions(string mechanicName, string expectedQuestion)
    {
        // Act
        var mechanic = _knowledgeBase.GetMechanic(mechanicName);

        // Assert
        mechanic.Should().NotBeNull();
        mechanic!.CommonQuestions.Should().ContainKey(expectedQuestion);
    }

    [Theory]
    [InlineData("freeze", "chill")]
    [InlineData("shock", "lightning_damage")]
    [InlineData("stun", "heavy_stun")]
    [InlineData("crit", "crit_multiplier")]
    [InlineData("spirit", "auras")]
    [InlineData("increased_vs_more", "support_gems")]
    public void Mechanics_HaveRelatedMechanics(string mechanicName, string expectedRelated)
    {
        // Act
        var mechanic = _knowledgeBase.GetMechanic(mechanicName);

        // Assert
        mechanic.Should().NotBeNull();
        mechanic!.RelatedMechanics.Should().Contain(expectedRelated);
    }
}
