using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace Covid19TempRecordApp_202003.Pages
{
    public class IndexModel : PageModel
    {
        [BindProperty]
        public List<SelectListItem> ClassIds { get; set; }
        public List<SelectListItem> StudentIds { get; set; }
        public List<UserEntity> UserEntities { get; set; }

        public string UIMsg { get; set; }

        private readonly ILogger<IndexModel> _logger;
        private readonly IConfiguration _configuration;
        private readonly CloudTableClient _tableClient;
        private readonly string _tableName;
        private readonly CloudTable _table;

        public IndexModel(ILogger<IndexModel> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;

            var StorageConnectionString = _configuration["StorageConnectionString"];
            var storageAccount = CloudStorageAccount.Parse(StorageConnectionString);
            _tableClient = storageAccount.CreateCloudTableClient(new TableClientConfiguration());

            _tableName = _configuration["TableName"];
            _table = _tableClient.GetTableReference(_tableName);
        }

        public void InitializeForm()
        {
            ClassIds = new List<SelectListItem>
            {
                new SelectListItem { Value = "1A", Text = "1年A組" },
                new SelectListItem { Value = "1B", Text = "1年B組" },
                new SelectListItem { Value = "1C", Text = "1年C組" }
            };

            StudentIds = new List<SelectListItem>();
            for (int i = 1; i <= 20; i++)
            {
                StudentIds.Add(new SelectListItem { Value = i.ToString("00"), Text = i.ToString() });
            }
        }

        public void OnGet()
        {
            InitializeForm();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var entity = new UserEntity
            {
                //PartitionKey = DateTime.Now.ToString("yyyyMMdd"),
                PartitionKey = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, TimeZoneInfo.FindSystemTimeZoneById("Tokyo Standard Time")).ToString("yyyyMMdd"),
                RowKey = Request.Form["ClassId"].ToString() + Request.Form["StudentId"].ToString(),
                Temperature = Convert.ToDouble(Request.Form["Temperature"]),
                Memo = Request.Form["Memo"].ToString(),
            };

            try
            {
                await InsertTableItemAsync(_table, entity);
                UIMsg = "体温データをアップロードしました";

                UserEntities = await GetTableItemsAsync(_table, entity.RowKey);
            }
            catch (Exception e)
            {
                UIMsg = "アップロードに失敗しました。もう一度やり直してください: ";
            }


            InitializeForm();
            return Page();
        }

        public async Task InsertTableItemAsync(CloudTable table, TableEntity entity)
        {
            var operation = TableOperation.InsertOrMerge(entity);
            await table.ExecuteAsync(operation);
        }

        public async Task<List<UserEntity>> GetTableItemsAsync(CloudTable table,string rowKey)
        {
            var query = new TableQuery<UserEntity>().Where(
                TableQuery.GenerateFilterCondition("RowKey", QueryComparisons.Equal, rowKey));
            var entities = table.ExecuteQuery(query).ToList();
            return entities;
        }
    }
}
