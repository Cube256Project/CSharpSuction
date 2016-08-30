using System.IO;

namespace CSharpSuction.Input
{
    class ConvertedInputInfo : SourceInfo
    {
        private string _template;

        public override string Template {  get { return _template; } }

        public ConvertedInputInfo(Suction suction, string fullpath) 
            : base(suction, fullpath)
        {
            if(fullpath.EndsWith(".xaml"))
            {
                // TODO: this is a temporary solution perhaps ...
                if (Path.GetFileName(fullpath) == "App.xaml")
                {
                    _template = "ApplicationDefinition";
                }
                else
                {
                    _template = "Page";
                }
            }
        }
    }
}
