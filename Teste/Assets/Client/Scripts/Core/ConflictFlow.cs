using System;

namespace L5RGame
{
    public class ConflictFlow : BaseStepWithPipeline, IGameStep
    {
        private Conflict conflict;
        private bool canPass;

        public ConflictFlow(Game game) : base(game) { }

        public ConflictFlow(Game game, Conflict currentConflict, bool allowPass) : base(game)
        {
            conflict = currentConflict;
            canPass = allowPass;
        }

        bool IGameStep.IsComplete() => IsComplete;
        bool IGameStep.CanCancel => CanCancel;

        public override string GetDebugInfo()
        {
            var conflictInfo = conflict != null ? $" - {conflict.conflictType}" : "";
            return $"ConflictFlow{conflictInfo} - Can pass: {canPass}";
        }
    }
}