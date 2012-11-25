using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.SharePoint.Client;
using NLog;
using File = System.IO.File;

namespace Eirikb.SharePoint.JSONList
{
    internal class Export : JsonListCommand
    {
        private readonly Regex _doNotWantFields = new Regex("counter|computed|mod|meta|guid", RegexOptions.IgnoreCase);
        private readonly Logger _log = Log.Current();
        private string _url;

        public Export()
        {
            IsCommand("Export", "Export all lists from a given SPWeb into a JSON file");
            HasRequiredOption("u|url=", "URL to SPWeb", u => _url = u);
        }

        public override int Run(string[] remainingArguments)
        {
            _log.Info("Connecting to {0}", _url);

            var ctx = new ClientContext(_url);
            _log.Info("Loading lists");
            var allLists = ctx.Web.Lists;
            ctx.Load(allLists);
            ctx.Load(ctx.Web);
            ctx.ExecuteQuery();

            var lists = allLists.ToList().Where(list => !list.IsCatalog).ToList();
            lists.ForEach(list => ctx.Load(list.Fields));
            ctx.ExecuteQuery();

            _log.Info("Exporting lists:");

            var data = lists.Select(list =>
                {
                    _log.Info("{0} ({1})", list.Title, list.ItemCount);
                    var fields =
                        list.Fields.ToList()
                            .Where(field => !_doNotWantFields.IsMatch(field.TypeAsString))
                            .Where(field => !field.ReadOnlyField)
                            .Select(field => field)
                            .ToList();

                    fields.Add(list.Fields.ToList().First(field => field.InternalName == "ID"));

                    var items = list.GetItems(new CamlQuery());
                    ctx.Load(items);
                    ctx.Load(list.RootFolder);
                    ctx.ExecuteQuery();

                    return new List
                        {
                            Name = list.RootFolder.Name,
                            Fields = fields.Select(field => field.InternalName).ToList(),
                            Items = items.ToList().Select(item => fields.Select(field =>
                                {
                                    var lValue = item[field.InternalName] as FieldLookupValue;
                                    if (lValue != null) return lValue.LookupId;
                                    var uValue = item[field.InternalName] as FieldUserValue;
                                    if (uValue != null) return uValue.LookupId;
                                    return item[field.InternalName];
                                }).ToList()).ToList()
                        };
                });

            var jss = new JavaScriptSerializer
                {
                    MaxJsonLength = int.MaxValue
                };
            var title = ctx.Web.Title;
            if (string.IsNullOrEmpty(title)) title = "NoName";
            _log.Info(title);
            File.WriteAllText(string.Format("{0}.json", title), jss.Serialize(data));
            _log.Info("Done. {0}", lists.Count);
            return 0;
        }
    }
}