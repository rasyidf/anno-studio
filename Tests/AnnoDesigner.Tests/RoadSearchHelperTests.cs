using AnnoDesigner.Core.Layout;
using AnnoDesigner.Core.Layout.Models;
using AnnoDesigner.Core.Models;
using AnnoDesigner.Helper;
using AnnoDesigner.Models;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Abstractions;
using System.Linq;
using Xunit;

namespace AnnoDesigner.Tests
{
    public class RoadSearchHelperTests
    {
        private readonly IFileSystem _fileSystem = new FileSystem();
        private static readonly LayoutFile defaultObjectList;
        static RoadSearchHelperTests()
        {
            defaultObjectList = new LayoutLoader().LoadLayout(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "RoadSearchHelper", "BreadthFirstSearch_FindBuildingInfluenceRange.ad"), true);
             
        }
        private string GetTestDataFile(string testCase)
        {
            return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "TestData", "RoadSearchHelper", $"{testCase}.ad");
        }

        [Fact]
        public void PrepareGridDictionary_SequenceIsNull_ShouldReturnNull_Issue197()
        {
            // Arrange/Act
            Moved2DArray<AnnoObject> gridDictionary = RoadSearchHelper.PrepareGridDictionary(null);

            // Assert
            Assert.Null(gridDictionary);
        }

        [Fact]
        public void PrepareGridDictionary_SequenceIsEmpty_ShouldReturnNull_Issue197()
        {
            // Arrange
            IEnumerable<AnnoObject> inputSequence = Enumerable.Empty<AnnoObject>();

            // Act
            Moved2DArray<AnnoObject> gridDictionary = RoadSearchHelper.PrepareGridDictionary(inputSequence);

            // Assert
            Assert.Null(gridDictionary);
        }

        [Fact]
        public void PrepareGridDictionary_SingleObject()
        {
            // Arrange
            List<AnnoObject> placedObjects = new LayoutLoader().LoadLayout(GetTestDataFile("PrepareGridDictionary_SingleObject"), true).Objects;
            AnnoObject[][] expectedResult = new AnnoObject[][]
            {
                new AnnoObject[5],
                new AnnoObject[]
                {
                    null,
                    placedObjects[0],
                    placedObjects[0],
                    placedObjects[0],
                    null
                },
                new AnnoObject[]
                {
                    null,
                    placedObjects[0],
                    placedObjects[0],
                    placedObjects[0],
                    null
                },
                new AnnoObject[5]
            };

            // Act
            Moved2DArray<AnnoObject> gridDictionary = RoadSearchHelper.PrepareGridDictionary(placedObjects);

            // Assert
            Assert.Equal(expectedResult, gridDictionary);
            Assert.Equal(0, gridDictionary.Offset.x);
            Assert.Equal(0, gridDictionary.Offset.y);
        }

        [Fact]
        public void PrepareGridDictionary_MultipleObjects()
        {
            // Arrange
            List<AnnoObject> placedObjects = new LayoutLoader().LoadLayout(GetTestDataFile("PrepareGridDictionary_MultipleObjects"), true).Objects;
            AnnoObject placedObject1 = placedObjects.FirstOrDefault(o => o.Label == "Object1");
            AnnoObject placedObject2 = placedObjects.FirstOrDefault(o => o.Label == "Object2");
            AnnoObject[][] expectedResult = new AnnoObject[][]
            {
                new AnnoObject[5],
                new AnnoObject[]
                {
                    null,
                    placedObject1,
                    placedObject1,
                    placedObject1,
                    null
                },
                new AnnoObject[]
                {
                    null,
                    placedObject1,
                    placedObject1,
                    placedObject1,
                    null
                },
                new AnnoObject[5],
                new AnnoObject[5],
                new AnnoObject[]
                {
                    null,
                    null,
                    placedObject2,
                    placedObject2,
                    null
                },
                new AnnoObject[5]
            };

            // Act
            Moved2DArray<AnnoObject> gridDictionary = RoadSearchHelper.PrepareGridDictionary(placedObjects);

            // Assert
            Assert.Equal(expectedResult, gridDictionary);
            Assert.Equal(0, gridDictionary.Offset.x);
            Assert.Equal(0, gridDictionary.Offset.y);
        }

        [Fact]
        public void PrepareGridDictionary_MultipleObjectsWithNegativeCoordinates()
        {
            // Arrange
            List<AnnoObject> placedObjects = new LayoutLoader().LoadLayout(GetTestDataFile("PrepareGridDictionary_MultipleObjectsWithNegativeCoordinates"), true).Objects;

            // Act
            Moved2DArray<AnnoObject> gridDictionary = RoadSearchHelper.PrepareGridDictionary(placedObjects);

            // Assert
            Assert.Equal(-10, gridDictionary.Offset.x);
            Assert.Equal(-5, gridDictionary.Offset.y);
        }

        [Fact]
        public void BreadthFirstSearch_FindObjectsInInfluenceRange()
        {
            // Arrange
            List<AnnoObject> placedObjects = new LayoutLoader().LoadLayout(GetTestDataFile("BreadthFirstSearch_FindObjectsInInfluenceRange"), true).Objects;
            List<AnnoObject> startObjects = placedObjects.Where(o => o.Label == "Start").ToList();

            // Act
            List<AnnoObject> objectsInInfluence = [];
            _ = RoadSearchHelper.BreadthFirstSearch(placedObjects, startObjects, o => (int)o.InfluenceRange + 1, inRangeAction: objectsInInfluence.Add);

            // Assert
            Assert.Equal(placedObjects.Where(o => o.Label == "TargetIn").ToHashSet(), [.. objectsInInfluence]);
            Assert.True(placedObjects.Where(o => o.Label == "TargetOut").All(o => !objectsInInfluence.Contains(o)));
        }

        [Fact]
        public void BreadthFirstSearch_FindBuildingInfluenceRange()
        {
            // Arrange
            List<AnnoObject> placedObjects = defaultObjectList.Objects;
            List<AnnoObject> startObjects = placedObjects.Where(o => o.Label == "Start").ToList();
            foreach (AnnoObject startObject in startObjects)
            {
                int expectedCount = (4 * Enumerable.Range(1, (int)startObject.InfluenceRange).Sum()) + 1;

                // Act
                bool[][] visitedCells = RoadSearchHelper.BreadthFirstSearch(placedObjects, new[] { startObject }, o => (int)o.InfluenceRange);

                // Assert
                Assert.Equal(expectedCount, visitedCells.Sum(c => c.Count(visited => visited)));
            }
        }

        [Fact]
        public void BreadthFirstSearch_StartObjectCountIsZero_ShouldReturnEmptyResult()
        {
            // Arrange
            List<AnnoObject> placedObjects = defaultObjectList.Objects;
            IEnumerable<AnnoObject> startObjects = Enumerable.Empty<AnnoObject>();

            bool[][] expectedResult = new bool[0][];

            // Act
            bool[][] visitedCells = RoadSearchHelper.BreadthFirstSearch(placedObjects, startObjects, o => (int)o.InfluenceRange);

            // Assert
            Assert.Equal(expectedResult, visitedCells);

        }

        [Fact]
        public void BreadthFirstSearch_PlacedObjectsEmpty_ShouldReturnEmptyResult_Issue197()
        {
            // Arrange
            IEnumerable<AnnoObject> placedObjects = Enumerable.Empty<AnnoObject>();
            List<AnnoObject> startObjects = defaultObjectList.Objects;

            bool[][] expectedResult = new bool[0][];

            // Act
            bool[][] visitedCells = RoadSearchHelper.BreadthFirstSearch(placedObjects, startObjects, o => (int)o.InfluenceRange);

            // Assert
            Assert.Equal(expectedResult, visitedCells);

        }

        [Fact]
        public void BreadthFirstSearch_PlacedObjectsNull_ShouldReturnEmptyResult_Issue197()
        {
            // Arrange
            IEnumerable<AnnoObject> placedObjects = null;
            List<AnnoObject> startObjects = defaultObjectList.Objects;

            bool[][] expectedResult = new bool[0][];

            // Act
            bool[][] visitedCells = RoadSearchHelper.BreadthFirstSearch(placedObjects, startObjects, o => (int)o.InfluenceRange);

            // Assert
            Assert.Equal(expectedResult, visitedCells);

        }
    }
}
