using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using DocSearchAIO.Classes;
using LiteDB;

namespace DocSearchAIO.Scheduler
{
    public class Comparers<T> where T : ElasticDocument
    {
        private readonly ILiteCollection<ComparerObject> _col;
        private readonly ILiteDatabase _liteDatabase;
        
        public Comparers(ILiteDatabase liteDatabase)
        {
            _col = liteDatabase.GetCollection<ComparerObject>($"cmp_{nameof(T)}");
            _liteDatabase = liteDatabase;
            _col.EnsureIndex(x => x.PathHash);
        }

        public bool RemoveNamedCollection()
        {
            return _liteDatabase.DropCollection($"cmp_{nameof(T)}");
        }
        
        public async Task<Maybe<T>> FilterExistingUnchanged(Maybe<T> document)
        {
            return await Task.Run(() =>
            {
                var opt = document.Bind(doc =>
                {
                    
                    var contentHash = doc.ContentHash;
                    var pathHash = doc.Id;
                    var originalFilePath = doc.OriginalFilePath;
                    return _col
                        .FindOne(comp => comp.PathHash == pathHash)
                        .MaybeValue()
                        .Match(
                            innerDoc =>
                            {
                                if (innerDoc.DocumentHash == contentHash)
                                    return Maybe<T>.None;

                                innerDoc.DocumentHash = contentHash;
                                _col.Update(innerDoc);
                                return Maybe<T>.From(doc);
                            },
                            () =>
                            {
                                var innerDocument = new ComparerObject
                                {
                                    DocumentHash = contentHash,
                                    PathHash = pathHash,
                                    OriginalPath = originalFilePath
                                };
                                _col.Insert(innerDocument);
                                return Maybe<T>.From(doc);
                            });
                });
                return opt;
            });
        }
        

    }
}