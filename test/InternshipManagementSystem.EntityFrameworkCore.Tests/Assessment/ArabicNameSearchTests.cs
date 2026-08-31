using System;
using System.Threading.Tasks;
using InternshipManagementSystem.Assessment.People;
using InternshipManagementSystem.Assessment.People.Dtos;
using Shouldly;
using Volo.Abp.MultiTenancy;
using Xunit;

namespace InternshipManagementSystem.EntityFrameworkCore.Assessment;

/// <summary>
/// Finding somebody whose name is written the other way.
/// <para>
/// Arabic writes one name several ways without changing it: four spellings of
/// alef, a final ha typed either <c>ة</c> or <c>ه</c>, and optional vowel marks
/// that almost nobody types and that a careful registrar sometimes does.
/// </para>
/// <para>
/// The marks are the hard case, and they are not a collation problem — which is
/// the finding that produced this column. A database matches a substring
/// positionally, and a fatha is a character sitting <em>between</em> two
/// letters, so «مُحَمَّد» cannot be found by «محمد» under any collation at all.
/// It was tried on the real server against <c>Arabic_CI_AI</c> and
/// <c>Latin1_General_CI_AI</c>; both say no. Only a folded copy of the name can
/// answer this, which is what the entity now keeps.
/// </para>
/// </summary>
public class ArabicNameSearchTests : InternshipManagementSystemEntityFrameworkCoreTestBase
{
    private readonly ICandidateAppService _candidates;
    private readonly ICurrentTenant _currentTenant;

    private static readonly Guid Tenant = Guid.Parse("cccccccc-0000-0000-0000-0000000000f1");

    public ArabicNameSearchTests()
    {
        _candidates = GetRequiredService<ICandidateAppService>();
        _currentTenant = GetRequiredService<ICurrentTenant>();
    }

    [Theory]
    [InlineData("مُحَمَّد الأحمد", "محمد")]      // written with vowel marks
    [InlineData("محمّد الأحمد", "محمد")]         // written with one
    [InlineData("محمد الاحمد", "محمد الأحمد")]   // hamza on the alef, or not
    [InlineData("فاطمه الزهراء", "فاطمة")]       // the final ha, either way
    [InlineData("عليٰ الهاشمي", "علي")]           // a superscript alef
    public async Task A_name_written_the_other_way_is_still_found(string stored, string typed)
    {
        await AsTenantAsync(async () =>
        {
            var code = Guid.NewGuid().ToString("N")[..8];

            await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = stored,
                Email = code + "@example.test",
            });

            var found = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                Filter = typed,
                MaxResultCount = 50,
            });

            found.Items.ShouldContain(c => c.FullName == stored);
        });
    }

    [Fact]
    public async Task Somebody_else_is_still_somebody_else()
    {
        await AsTenantAsync(async () =>
        {
            var code = Guid.NewGuid().ToString("N")[..8];

            await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "خديجة القاسمي",
                Email = code + "@example.test",
            });

            var found = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                Filter = "محمد",
                MaxResultCount = 50,
            });

            // Folding must not become finding everybody. A search that matches
            // more than it should is a search a coordinator stops trusting, and
            // then stops using.
            found.Items.ShouldNotContain(c => c.FullName == "خديجة القاسمي");
        });
    }

    [Fact]
    public async Task A_latin_name_searches_exactly_as_it_did()
    {
        await AsTenantAsync(async () =>
        {
            var code = Guid.NewGuid().ToString("N")[..8];

            await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "Layla Hassan",
                Email = code + "@example.test",
            });

            var found = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                Filter = "Hassan",
                MaxResultCount = 50,
            });

            // The other half of the promise: nothing that worked before stops
            // working. Most of this product's customers write some names in
            // Latin script and some in Arabic, in the same roll.
            found.Items.ShouldContain(c => c.FullName == "Layla Hassan");
        });
    }

    [Fact]
    public async Task Correcting_a_name_corrects_what_can_be_searched_for()
    {
        await AsTenantAsync(async () =>
        {
            var code = Guid.NewGuid().ToString("N")[..8];

            var person = await _candidates.CreateAsync(new CreateUpdateCandidateDto
            {
                FullName = "خالد",
                Email = code + "@example.test",
            });

            await _candidates.UpdateAsync(person.Id, new CreateUpdateCandidateDto
            {
                FullName = "مُحَمَّد",
                Email = code + "@example.test",
            });

            var found = await _candidates.GetListAsync(new CandidateListRequestDto
            {
                Filter = "محمد",
                MaxResultCount = 50,
            });

            // The reason the folded copy is written by the property setter and
            // not by whoever remembers to. A search column that can drift from
            // the name it indexes is worse than none: it finds people who have
            // been renamed and misses people who have not.
            found.Items.ShouldContain(c => c.Id == person.Id);
        });
    }

    private async Task AsTenantAsync(Func<Task> action)
    {
        using (_currentTenant.Change(Tenant))
        {
            await WithUnitOfWorkAsync(action);
        }
    }
}
