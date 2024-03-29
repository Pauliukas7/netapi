namespace Catalog.UnitTests;

using Catalog.Api.Controllers;
using Catalog.Api.DTOs;
using Catalog.Api.Entities;
using Catalog.Api.Repositories;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;

public class ItemsControllerTests
{
    private readonly Mock<IItemsRepository> repositoryStub = new();
    private readonly Mock<ILogger<ItemsController>> loggerStub = new();
    private readonly Random rand = new();

    [Fact]
    public async Task GetItemAsync_WithUnexistingItem_ReturnsNotFound()
    {
        repositoryStub.Setup(repo => repo.GetItemAsync(It.IsAny<Guid>())).ReturnsAsync((Item)null);

        var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

        var result = await controller.GetItemAsync(Guid.NewGuid());

        result.Result.Should().BeOfType<NotFoundResult>();
    }

    [Fact]
    public async Task GetItemAsync_WithExistingItem_ReturnsExpectedItem()
    {
        var expectedItem = CreateRandomItem();

        repositoryStub
            .Setup(repo => repo.GetItemAsync(It.IsAny<Guid>()))
            .ReturnsAsync(expectedItem);
        var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);
        var result = await controller.GetItemAsync(Guid.NewGuid());

        result.Value.Should().BeEquivalentTo(expectedItem);
    }

    [Fact]
    public async Task GetItemsAsync_WithExistingItems_ReturnsAllItems()
    {
        var expectedItems = new[] { CreateRandomItem(), CreateRandomItem(), CreateRandomItem() };

        repositoryStub.Setup(repo => repo.GetItemsAsync()).ReturnsAsync(expectedItems);

        var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);
        var actualItems = await controller.GetItemsAsync();

        actualItems.Should().BeEquivalentTo(expectedItems);
    }

    [Fact]
    public async Task CreateItemAsync__WithItemToCreate_ReturnsCreateItem()
    {
        var itemToCreate = new CreateItemDto(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            rand.Next(1000)
        );

        var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

        var result = await controller.CreateItemAsync(itemToCreate);

        var createdItem = (result.Result as CreatedAtActionResult).Value as ItemDto;
        itemToCreate
            .Should()
            .BeEquivalentTo(
                createdItem,
                options => options.ComparingByMembers<ItemDto>().ExcludingMissingMembers()
            );
        createdItem.Id.Should().NotBeEmpty();
        createdItem.CreatedDate.Should().BeCloseTo(DateTimeOffset.UtcNow, new TimeSpan(0, 0, 1000));
    }

    [Fact]
    public async Task UpdateItemAsync__WithExistingItem_ReturnsNoContent()
    {
        Item existingItem = CreateRandomItem();
        repositoryStub
            .Setup(repo => repo.GetItemAsync(It.IsAny<Guid>()))
            .ReturnsAsync(existingItem);

        var itemId = existingItem.Id;
        var itemToUpdate = new UpdateItemDto(
            Guid.NewGuid().ToString(),
            Guid.NewGuid().ToString(),
            existingItem.Price + 3
        );

        var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

        var result = await controller.UpdateItemAsync(itemId, itemToUpdate);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task DeleteItemAsync__WithExistingItem_ReturnsNoContent()
    {
        Item existingItem = CreateRandomItem();
        repositoryStub
            .Setup(repo => repo.GetItemAsync(It.IsAny<Guid>()))
            .ReturnsAsync(existingItem);

        var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

        var result = await controller.DeleteItemAsync(existingItem.Id);

        result.Should().BeOfType<NoContentResult>();
    }

    [Fact]
    public async Task GetItemsAsync_WithMatchingItems_ReturnsMatchingItems()
    {
        var allItems = new[]
        {
            new Item() { Name = "Potion" },
            new Item() { Name = "Sword" },
            new Item() { Name = "Hi-Potion" }
        };

        var nameToMatch = "Potion";

        repositoryStub.Setup(repo => repo.GetItemsAsync()).ReturnsAsync(allItems);

        var controller = new ItemsController(repositoryStub.Object, loggerStub.Object);

        IEnumerable<ItemDto> foundItems = await controller.GetItemsAsync(nameToMatch);

        foundItems.Should().OnlyContain(item => item.Name.Contains(nameToMatch));
    }

    private Item CreateRandomItem()
    {
        return new()
        {
            Id = Guid.NewGuid(),
            Name = Guid.NewGuid().ToString(),
            Price = rand.Next(1000),
            CreatedDate = DateTimeOffset.UtcNow
        };
    }
}
