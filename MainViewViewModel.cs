using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Structure;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Selection;
using Prism.Commands;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RevitAPIFurniture
{
    public class MainViewViewModel
    {
        private ExternalCommandData _commandData;
        public List<FamilySymbol> FamilyTypes { get; } = new List<FamilySymbol>();
        public DelegateCommand PickCommand { get; }
        public List<Level> Levels { get; } = new List<Level>();
        public FamilySymbol SelectedFamilyType { get; set; }
        public Level SelectedLevel { get; set; }

        public MainViewViewModel(ExternalCommandData commandData)
        {
            _commandData = commandData;
            FamilyTypes = GetFamilySymbols();
            PickCommand = new DelegateCommand(OnPickCommand);
            Levels = GetLevels();
        }

        private List<Level> GetLevels()
        {
            Document doc = _commandData.Application.ActiveUIDocument.Document;
            List<Level> levels = new FilteredElementCollector(doc)
                                                .OfClass(typeof(Level))
                                                .Cast<Level>()
                                                .ToList();
            return levels;
        }

        private List<FamilySymbol> GetFamilySymbols()
        {
            Document doc = _commandData.Application.ActiveUIDocument.Document;
            var familySymbols = new FilteredElementCollector(doc)
                .OfClass(typeof(FamilySymbol))
                .OfCategory(BuiltInCategory.OST_Furniture)
                .Cast<FamilySymbol>()
                .ToList();

            return familySymbols;
        }

        private void OnPickCommand()
        {
            Document doc = _commandData.Application.ActiveUIDocument.Document;

            if (SelectedFamilyType == null || SelectedLevel == null)
                return;

            Level level = (Level)doc.GetElement(SelectedLevel.Id);
            FamilySymbol familySymbol = (FamilySymbol)doc.GetElement(SelectedFamilyType.Id);

            RaiseCloseRequest();
            CreateFamilyInstance(familySymbol, GetPoint(), level);

        }

        private XYZ GetPoint()
        {
            UIDocument uidoc = _commandData.Application.ActiveUIDocument;
            XYZ point = uidoc.Selection.PickPoint(ObjectSnapTypes.Endpoints, "Выберите точку");
            return point;
        }

        private FamilyInstance CreateFamilyInstance(FamilySymbol familySymbol, XYZ pickPoint, Level level)
        {
            Document doc = _commandData.Application.ActiveUIDocument.Document;
            FamilyInstance familyInstance = null;

            using (var ts = new Transaction(doc, "Create family instance"))
            {
                ts.Start();
                if (!familySymbol.IsActive)
                {
                    familySymbol.Activate();
                    doc.Regenerate();
                }
                familyInstance = doc.Create.NewFamilyInstance(pickPoint, familySymbol, level, StructuralType.NonStructural);
                ts.Commit();
            }
            return familyInstance;
        }

        public event EventHandler CloseRequest;
        private void RaiseCloseRequest()
        {
            CloseRequest?.Invoke(this, EventArgs.Empty);
        }

    }
}
