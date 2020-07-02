using System;
using AuditLogService.UnitTest.Models;
using ISFG.Alfresco.Api.Interfaces;
using ISFG.Alfresco.Api.Models;
using ISFG.Alfresco.Api.Models.CoreApi.CoreApi;
using ISFG.Alfresco.Api.Services;
using NUnit.Framework;

namespace AuditLogService.UnitTest
{
    [TestFixture]
    public class AlfrescoModelComparerUnitTest
    {
        #region Fields

        private IAlfrescoModelComparer _alfrescoModelComparer;

        #endregion

        #region Public Methods

        [Test]
        public void CompareProperties_WhenIsCalled_ReturnNestedProperties()
        {
            var obj1 = AlfrescoComparerModels.NestedProperties1();
            var obj2 = AlfrescoComparerModels.NestedProperties2();

            var result = _alfrescoModelComparer.CompareProperties(obj1, obj2);
            
            Assert.That(result, Has.Exactly(4).Items);
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.New));
            Assert.That(result, Has.Exactly(2).Matches<ObjectDifference>(x => x.Operation == Operations.Edit));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.Deleted));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "ssl:newValue" && x.OldValue == null && x.NewValue.ToString() == "new"));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "ssl:deletedValue" && x.OldValue.ToString() == "delete" && x.NewValue == null));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "ssl:customBool" && Convert.ToBoolean(x.OldValue) && Convert.ToBoolean(x.NewValue) == false));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "ssl:nestedValue.displayName" && x.OldValue.ToString() == "martin" && x.NewValue.ToString() == "karel"));
        }

        [Test]
        public void CompareProperties_WhenIsCalled_ReturnProperties()
        {
            var obj1 = AlfrescoComparerModels.Properties1();
            var obj2 = AlfrescoComparerModels.Properties2();

            var result = _alfrescoModelComparer.CompareProperties(obj1, obj2);
            
            Assert.That(result, Has.Exactly(3).Items);
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.New));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.Edit));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.Deleted));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "ssl:newValue" && x.OldValue == null && x.NewValue.ToString() == "new"));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "ssl:deletedValue" && x.OldValue.ToString() == "delete" && x.NewValue == null));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "ssl:customBool" && Convert.ToBoolean(x.OldValue) && Convert.ToBoolean(x.NewValue) == false));
        }

        [Test]
        public void ObjectDifference_WhenIsCalled_ReturnDeletedValues()
        {
            var obj1 = AlfrescoComparerModels.DeletedObject1();
            var obj2 = AlfrescoComparerModels.DeletedObject2();
            
            var result = _alfrescoModelComparer.CompareObjects(obj1, obj2);
            
            Assert.That(result, Has.Exactly(2).Items);
            Assert.That(result, Has.Exactly(2).Matches<ObjectDifference>(x => x.Operation == Operations.Deleted));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.Id" && x.OldValue.ToString() == "Different" && x.NewValue == null));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.IsFavorite" && Convert.ToBoolean(x.OldValue) && x.NewValue == null));
        }

        [Test]
        public void ObjectDifference_WhenIsCalled_ReturnEditedValues()
        {
            var obj1 = AlfrescoComparerModels.EditedObject1();
            var obj2 = AlfrescoComparerModels.EditedObject2();
            
            var result = _alfrescoModelComparer.CompareObjects(obj1, obj2);
            
            Assert.That(result, Has.Exactly(3).Items);
            Assert.That(result, Has.Exactly(3).Matches<ObjectDifference>(x => x.Operation == Operations.Edit));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.Id" && x.OldValue.ToString() == "Id" && x.NewValue.ToString() == "Different"));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.NodeType" && x.OldValue.ToString() == "NodeType" && x.NewValue.ToString() == "Different"));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.IsFavorite" && Convert.ToBoolean(x.OldValue) && Convert.ToBoolean(x.NewValue) == false));
        }

        [Test]
        public void ObjectDifference_WhenIsCalled_ReturnNewValues()
        {
            var obj1 = AlfrescoComparerModels.NewObject1();
            var obj2 = AlfrescoComparerModels.NewObject2();
            
            var result = _alfrescoModelComparer.CompareObjects(obj1, obj2);
            
            Assert.That(result, Has.Exactly(2).Items);
            Assert.That(result, Has.Exactly(2).Matches<ObjectDifference>(x => x.Operation == Operations.New));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.Id" && x.OldValue == null && x.NewValue.ToString() == "Different"));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.IsFavorite" && x.OldValue == null && Convert.ToBoolean(x.NewValue)));
        }

        [Test]
        public void ObjectDifference_WhenIsCalled_ReturnNoDifference()
        {
            var obj1 = AlfrescoComparerModels.SameObject();
            var obj2 = AlfrescoComparerModels.SameObject();

            var result = _alfrescoModelComparer.CompareObjects(obj1, obj2);
            
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void ObjectDifference_WhenIsCalled_ReturnNoDifferenceNull()
        {
            var result = _alfrescoModelComparer.CompareObjects<NodeEntry>(null, null);
            
            Assert.That(result.Count, Is.EqualTo(0));
        }

        [Test]
        public void ObjectDifference_WhenIsCalled_ReturnPrimitiveTypes()
        {
            var obj1 = AlfrescoComparerModels.PrimitiveTypes1();
            var obj2 = AlfrescoComparerModels.PrimitiveTypes2();
            
            var result = _alfrescoModelComparer.CompareObjects(obj1, obj2);
            
            Assert.That(result, Has.Exactly(3).Items);
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.New));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.Edit));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.Deleted));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.Id" && x.OldValue == null && x.NewValue.ToString() == "Different"));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.NodeType" && x.OldValue.ToString() == "NodeType" && x.NewValue == null));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.IsFile" && Convert.ToBoolean(x.OldValue) && Convert.ToBoolean(x.NewValue) == false));
        }

        [Test]
        public void ObjectDifference_WhenIsCalled_ReturnReferenceTypes()
        {
            var obj1 = AlfrescoComparerModels.ReferenceTypes1();
            var obj2 = AlfrescoComparerModels.ReferenceTypes2();
            
            var result = _alfrescoModelComparer.CompareObjects(obj1, obj2);
            
            Assert.That(result, Has.Exactly(3).Items);
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.New));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.Edit));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Operation == Operations.Deleted));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.CreatedByUser.Id" && x.OldValue == null && x.NewValue.ToString() == "Different"));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.Content.MimeType" && x.OldValue.ToString() == "MimeType" && x.NewValue == null));
            Assert.That(result, Has.Exactly(1).Matches<ObjectDifference>(x => x.Key == "Entry.Content.Encoding" && x.OldValue.ToString() == "Encoding" && x.NewValue.ToString() == "Different"));
        }

        [Test]
        public void ObjectDifference_WhenIsCalled_ThrowException()
        {
            var obj1 = AlfrescoComparerModels.SameObject();
            
            Assert.Throws<Exception>(() => _alfrescoModelComparer.CompareObjects(obj1, null));
        }

        [SetUp]
        public void Setup()
        {
            _alfrescoModelComparer = new AlfrescoModelComparer();
        }

        [TearDown]
        public void Teardown()
        {
            _alfrescoModelComparer = null;
        }

        #endregion
    }
}