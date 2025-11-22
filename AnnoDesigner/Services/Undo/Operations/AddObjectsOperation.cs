using System.Collections.Generic;

namespace AnnoDesigner.Services.Undo.Operations
{
    public class AddObjectsOperation<T> : BaseOperation
    {
        public IEnumerable<T> Objects { get; set; }

        public ICollection<T> Collection { get; set; }

        protected override void UndoOperation()
        {
            foreach (var obj in Objects)
            {
                _ = Collection.Remove(obj);
            }
        }

        protected override void RedoOperation()
        {
            foreach (var obj in Objects)
            {
                Collection.Add(obj);
            }
        }
    }
}
