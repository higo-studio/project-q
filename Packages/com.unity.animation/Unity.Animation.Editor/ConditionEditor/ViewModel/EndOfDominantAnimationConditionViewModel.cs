using Unity.Animation.Model;
using UnityEditor.GraphToolsFoundation.Overdrive;

namespace Unity.Animation.Editor
{
    internal class EndOfDominantAnimationConditionViewModel : BaseConditionViewModel
    {
        public EndOfDominantAnimationConditionViewModel(EndOfDominantAnimationConditionModel model, IGraphAssetModel graphAssetModel)
            : base(model, graphAssetModel)
        {
        }

        internal EndOfDominantAnimationConditionModel EndOfDominantAnimationConditionModel => (EndOfDominantAnimationConditionModel)Model;
    }
}
