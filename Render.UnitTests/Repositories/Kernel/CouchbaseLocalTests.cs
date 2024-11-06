using Couchbase.Lite;
using Moq;
using Newtonsoft.Json;
using Render.Repositories.Kernel;
using Render.TempFromVessel.Kernel;
using Render.UnitTests.App.Kernel;

namespace Render.UnitTests.Repositories.Kernel;

public class CouchbaseLocalTests:TestBase
{
    private readonly Mock<IDatabaseWrapper> _mockDatabaseWrapper;
    private readonly CouchbaseLocal<TestEntity> _couchbaseLocal;
    
    public CouchbaseLocalTests()
    {
        _mockDatabaseWrapper = new Mock<IDatabaseWrapper>();
        _couchbaseLocal = new CouchbaseLocal<TestEntity>(_mockDatabaseWrapper.Object);
    }

    [Fact]
    public async Task GetAsync_EntityIsSoftDeleted_ReturnsNull()
    {
        // Arrange
        var deletedEntity = new TestEntity(1, true, Guid.NewGuid());
        var document = CreateTestDocument(deletedEntity);

        _mockDatabaseWrapper.Setup(db => db.GetDocument(It.IsAny<string>()))
            .Returns(document);

        // Act
        var result = await _couchbaseLocal.GetAsync("test_key");

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAsync_EntityIsNotDeleted_ReturnsEntity()
    {
        // Arrange
        var entity = new TestEntity(1, false, Guid.NewGuid());
        var document = CreateTestDocument(entity);

        _mockDatabaseWrapper.Setup(db => db.GetDocument("test_key"))
            .Returns(document);

        // Act
        var result = await _couchbaseLocal.GetAsync("test_key");

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEquivalentTo(entity, options => options.Excluding(x => x.DateUpdated));
    }
    
    private Document CreateTestDocument(TestEntity entity)
    {
        var mutableDoc = new MutableDocument();
        mutableDoc.SetBoolean("IsDeleted", entity.IsDeleted);
        mutableDoc.SetString("Id", entity.Id.ToString());
        mutableDoc.SetInt("DocumentVersion", 1);
        return mutableDoc;
    }
}
public class TestEntity : DomainEntity, ISoftDeletable 
{
    [JsonConstructor]
    protected internal TestEntity(int documentVersion, bool isDeleted, Guid id) : base(documentVersion)
    {
        IsDeleted = isDeleted;
        Id = id;
    }

    public bool IsDeleted { get; set; }
    public Guid Id { get; }
}
