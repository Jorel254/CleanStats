using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using System.Linq;

namespace CleanStats.Core.Model;

public enum ClassificationCategory
{
    Temporary,
    Cache,
    Log,
    Dump,
    Other
}
public enum ClassificationReason
{
    ExtensionMatchesTemporary,
    PathMatchesCacheDirectory,
    ExtensionMatchesLog,
    ExceedsAgeThreshold,
    MatchesKnownDumpExtension,
    RequiresManualReview,
    NoMatchingRule
}

public enum ClassificationStatus
{
    Unclassified,
    Classified,
    PendingReview
}
public class ClassificationResult
{
    public Guid FileEntryId { get; }
    public ClassificationCategory Category { get; }
    private readonly ObservableCollection<ClassificationReason> _reasons;

    public ReadOnlyObservableCollection<ClassificationReason> Reason { get; }
    public ClassificationStatus Status { get; }
    public DateTime ClassifiedAt { get; }

    public ClassificationResult(Guid fileEntryId, ClassificationCategory category, IEnumerable<ClassificationReason> reason, ClassificationStatus status)
    {
        if (!Enum.IsDefined(typeof(ClassificationCategory), category))
        {
            throw new ArgumentException("El tipo de categoría proporcionado no es válido.", nameof(category));
        }
        if (reason == null)
        {
            throw new ArgumentNullException(nameof(reason), "La colección de motivos no puede ser nula.");
        }
        if ( reason.Count() == 0 || reason.Any(r => !Enum.IsDefined(typeof(ClassificationReason), r)))
        {
            throw new ArgumentException("La colección de motivos contiene valores no válidos.", nameof(reason));
        }
        if (reason.Distinct().Count() != reason.Count())
        {
            throw new ArgumentException(
                "La colección de motivos contiene valores duplicados.",
                nameof(reason)
            );
        }
        if (!Enum.IsDefined(typeof(ClassificationStatus), status))
        {
            throw new ArgumentException("El estado de clasificación proporcionado no es válido.", nameof(status));
        }
        if (fileEntryId == Guid.Empty)
        {
            throw new ArgumentException("El ID de la entrada de archivo no puede ser un GUID vacío.", nameof(fileEntryId));
        }

        FileEntryId = fileEntryId;
        Category = category;
        _reasons = new ObservableCollection<ClassificationReason>(reason);
        Reason = new ReadOnlyObservableCollection<ClassificationReason>(_reasons);
        Status = status;
        ClassifiedAt = DateTime.UtcNow;
    }
}
