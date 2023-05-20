using System.Dynamic;
using RulesEngine.Models;

namespace RssFeeder.Console.Commands
{
    public class CheckRulesCommand : OaktonCommand<CheckRulesInput>
    {
        private readonly ILogger _log;
        private RulesEngine.RulesEngine _bre;

        public CheckRulesCommand(ILogger log)
        {
            _log = log;

            InitializeRulesEngine();
        }

        public override bool Execute(CheckRulesInput input)
        {
            dynamic x = new ExpandoObject();
            x.name = "site_name";
            x.text = "text";
            x.id = "id";
            x.tagname = "tagname";
            x.style = "style";
            x.classlist = "classlist";
            x.selector = "selector";
            x.parentclasslist = "parentclasslist";
            x.parenttagname = "parenttagname";
            var i = new dynamic[] { x };

            _log.Debug("Input = {@input}", i);

            List<RuleResultTree> resultList = new();
            resultList.AddRange(_bre.ExecuteAllRulesAsync("ExcludeUL", i).GetAwaiter().GetResult());
            resultList.AddRange(_bre.ExecuteAllRulesAsync("ExcludeHeader", i).Result);
            resultList.AddRange(_bre.ExecuteAllRulesAsync("ExcludeParagraph", i).Result);
            resultList.AddRange(_bre.ExecuteAllRulesAsync("ExcludeBlockquote", i).Result);

            //Check success for rule
            foreach (var result in resultList)
            {
                if (result.IsSuccess)
                {
                    _log.Information("Skipped tag: {tag} Reason: {reason}", x.tagname, result.Rule.RuleName);
                    return false;
                }
            }

            // Just telling the OS that the command
            // finished up okay
            _log.Information("Rules successfully validated. {workflows}", string.Join(';', _bre.GetAllRegisteredWorkflowNames().ToArray()));
            return true;
        }

        private void InitializeRulesEngine()
        {
            var files = Directory.GetFiles(Directory.GetCurrentDirectory(), "ExcludeContentRules.json", SearchOption.AllDirectories);
            if (files == null || files.Length == 0)
                throw new InvalidOperationException("Rules not found.");

            var fileData = File.ReadAllText(files[0]);
            var workflow = JsonConvert.DeserializeObject<List<Workflow>>(fileData) ?? new List<Workflow>();

            _bre = new RulesEngine.RulesEngine(workflow.ToArray(), null);
        }
    }
}
