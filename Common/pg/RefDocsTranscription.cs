using System;
using System.Collections.Generic;

namespace RV.InvNew.Common;

public partial class RefDocsTranscription
{
    public long Id { get; set; }

    public long RefDoc { get; set; }

    public string TranscribedContent { get; set; } = null!;

    public string TranscriberLlmName { get; set; } = null!;

    public DateTime TranscribedAt { get; set; }

    public string TranscriptionStructured { get; set; } = null!;

    public string TranscriptionStructureType { get; set; } = null!;

    public DateTime? RefDocIssuedAt { get; set; }

    public DateTime? RefDocValidFrom { get; set; }

    public DateTime? RefDocNotValidAfter { get; set; }

    public string RefDocSummary { get; set; } = null!;

    public string RefDocTitle { get; set; } = null!;

    public double? TranscriptionCostUsdc { get; set; }

    public long? InputAudioTokens { get; set; }

    public long? InputTextTokens { get; set; }

    public long? InputImageTokens { get; set; }

    public long? InputVideoTokens { get; set; }

    public long? OutputAudioTokens { get; set; }

    public long? OutputTextTokens { get; set; }

    public long? OutputImageTokens { get; set; }

    public long? OutputVideoTokens { get; set; }

    public string RequestOutputAsIs { get; set; } = null!;
}
