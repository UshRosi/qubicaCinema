using QubicaCinema.BuildingBlocks.Api.Paging;

namespace QubicaCinema.BuildingBlocks.UnitTests.Paging;

public sealed class PageQueryTests
{
    [Fact]
    public void Should_return_the_first_page_at_the_default_size_when_the_caller_does_not_say()
    {
        PageQuery query = new(Page: null, PageSize: null);

        query.Number.ShouldBe(1);
        query.Size.ShouldBe(PageQuery.DefaultSize);
        query.Skip.ShouldBe(0);
    }

    [Fact]
    public void Should_skip_every_item_on_the_pages_before_the_one_asked_for()
    {
        new PageQuery(Page: 3, PageSize: 10).Skip.ShouldBe(20);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Should_treat_a_page_below_one_as_the_first(int page)
    {
        new PageQuery(page, PageSize: null).Number.ShouldBe(1);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(1_000_000, PageQuery.MaxSize)]
    public void Should_clamp_the_page_size_into_range(int pageSize, int expected)
    {
        new PageQuery(Page: null, pageSize).Size.ShouldBe(expected);
    }

    [Theory]
    [InlineData(PageQuery.MaxNumber + 1)]
    [InlineData(int.MaxValue)]
    public void Should_stop_at_the_last_page_whose_offset_fits_in_an_int(int page)
    {
        PageQuery query = new(page, PageQuery.MaxSize);

        query.Number.ShouldBe(PageQuery.MaxNumber);
        query.Skip.ShouldBe((PageQuery.MaxNumber - 1) * PageQuery.MaxSize);
        query.Skip.ShouldBeGreaterThan(0);
    }
}
