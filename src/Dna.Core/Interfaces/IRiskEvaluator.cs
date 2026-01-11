using Dna.Core.Models;
using System.Collections.Generic;

namespace Dna.Core.Interfaces;

public interface IRiskEvaluator
{
    List<DetectedRisk> Evaluate(ProjectAnalysis analysis);
}