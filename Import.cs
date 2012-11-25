using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Web.Script.Serialization;
using Microsoft.SharePoint.Client;
using NLog;
using File = System.IO.File;

namespace Eirikb.SharePoint.JSONList
{
    internal class Import : JsonListCommand
    {
        private readonly Dictionary<string, Dictionary<int, int>> _remapedRelatedIds = new Dictionary<string, Dictionary<int, int>>();
        private readonly Logger _log = Log.Current();
        private bool _clearLists;
        private string _fileName;
        private string _url;
        private readonly Regex _doNotWantFields = new Regex("id|attachment" , RegexOptions.IgnoreCase);

        public Import()
        {
            IsCommand("Import", "Import all lists from a given SPWeb from a JSON file");
            HasRequiredOption("u|url=", "URL to SPWeb", u => _url = u);
            HasOption("c|clear", "Clear all data from lists when importing", c => { _clearLists = true; });
            HasOption("f|file=", "JSON File for importing, if blank uses SPWeb.Title", f => _fileName = f);
        }

        private void SetId(string name, int id, int actualId)
        {
            Dictionary<int, int> ids;
            if (!_remapedRelatedIds.TryGetValue(name, out ids)) ids = new Dictionary<int, int>();
            ids[id] = actualId;
            _remapedRelatedIds[name] = ids;
        }

        private int GetId(string name, int id)
        {
            Dictionary<int, int> ids;
            if (!_remapedRelatedIds.TryGetValue(name, out ids)) return -1;
            int actualId;
            if (!ids.TryGetValue(id, out actualId)) return -1;
            return actualId;
        }

        public override int Run(string[] remainingArguments)
        {
            _log.Info("Connecting to {0}...", _url);
            var ctx = new ClientContext(_url);
            ctx.Load(ctx.Web);
            ctx.ExecuteQuery();

            var jss = new JavaScriptSerializer();

            var fileName = _fileName ?? string.Format("{0}.json", ctx.Web.Title);
            _log.Info("Reading from {0}", fileName);
            var data = jss.Deserialize<List<List>>(File.ReadAllText(string.Format("{0}", fileName)));

            _log.Info("Loading lists");
            var allLists = ctx.Web.Lists;
            ctx.Load(allLists);
            ctx.ExecuteQuery();

            _log.Info("Loading fields");
            var lists = allLists.ToList().Where(list => !list.IsCatalog).ToList();
            lists.ForEach(list => ctx.Load(list.Fields));
            ctx.ExecuteQuery();

            _log.Info("Sorting lists");
            lists.Sort((a, b) =>
                {
                    var x = a.Fields.ToList().Any(field =>
                        {
                            var lookupField = field as FieldLookup;
                            if (lookupField == null || string.IsNullOrEmpty(lookupField.LookupList)) return false;

                            return new Guid(lookupField.LookupList) == b.Id;
                        });
                    return x ? 0 : 1;
                });

            _log.Info("Importing data");
            lists.ForEach(list => ctx.Load(list.RootFolder));
            ctx.ExecuteQuery();
            lists.ForEach(list =>
                {
                    var name = list.RootFolder.Name;
                    var dataList = data.FirstOrDefault(dl => dl.Name == name);
                    if (dataList == null)
                    {
                        _log.Info("  Not found: " + list.Title);
                        return;
                    }

                    _log.Info("  " + list.Title);

                    if (_clearLists)
                    {
                        _log.Info("    Clearing list");
                        var items = list.GetItems(new CamlQuery());
                        ctx.Load(items);
                        ctx.ExecuteQuery();
                        items.ToList().ForEach(i => i.DeleteObject());
                        ctx.ExecuteQuery();
                    }

                    _log.Info("    Loading fields");
                    var fields = list.Fields;
                    ctx.Load(fields);
                    ctx.ExecuteQuery();

                    _log.Info("    Importing data ({0})", dataList.Items.Count);
                    dataList.Items.ForEach(columns =>
                        {
                            var item = list.AddItem(new ListItemCreationInformation());
                            for (var i = 0; i < columns.Count; i++)
                            {
                                var field = dataList.Fields[i];
                                SetItemValue(fields, item, field, columns[i]);
                            }
                            item.Update();
                            ctx.ExecuteQuery();
                            var idIndex = dataList.Fields.FindIndex(c => c == "ID");
                            int id;
                            if (int.TryParse("" + columns[idIndex], out id)) SetId(name, id, item.Id);
                        });
                });

            return 0;
        }

        private void SetItemValue(FieldCollection fields, ListItem item, string field, object value)
        {
            if (_doNotWantFields.IsMatch(field)) return;

            item[field] = value;
            var lookupField = fields.GetByInternalNameOrTitle(field) as FieldLookup;
            if (lookupField == null) return;

            int lookupId;
            if (!int.TryParse("" + value, out lookupId)) return;

            lookupId = GetId(lookupField.LookupList, lookupId);
            if (lookupId >= 0) item[field] = lookupId;
        }
    }
}