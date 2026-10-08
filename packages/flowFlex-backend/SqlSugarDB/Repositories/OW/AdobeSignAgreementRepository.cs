using SqlSugar;
using FlowFlex.Domain.Entities.OW;
using FlowFlex.Domain.Repository.OW;
using FlowFlex.Domain.Shared;

namespace FlowFlex.SqlSugarDB.Repositories.OW
{
    /// <summary>
    /// Adobe Sign Agreement repository implementation (OW-731)
    /// </summary>
    public class AdobeSignAgreementRepository : BaseRepository<AdobeSignAgreement>, IAdobeSignAgreementRepository, IScopedService
    {
        public AdobeSignAgreementRepository(ISqlSugarClient db) : base(db)
        {
        }

        /// <inheritdoc />
        public async Task<AdobeSignAgreement?> GetBySourceFileIdAsync(long sourceFileId)
        {
            return await db.Queryable<AdobeSignAgreement>()
                .Where(a => a.SourceFileId == sourceFileId && a.IsValid == true)
                .OrderByDescending(a => a.CreateDate)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<List<AdobeSignAgreement>> GetByOnboardingIdAsync(long onboardingId)
        {
            return await db.Queryable<AdobeSignAgreement>()
                .Where(a => a.OnboardingId == onboardingId && a.IsValid == true)
                .OrderByDescending(a => a.CreateDate)
                .ToListAsync();
        }

        /// <inheritdoc />
        public async Task<AdobeSignAgreement?> GetByAgreementIdAsync(string adobeAgreementId)
        {
            return await db.Queryable<AdobeSignAgreement>()
                .Where(a => a.AgreementId == adobeAgreementId && a.IsValid == true)
                .FirstAsync();
        }

        /// <inheritdoc />
        public async Task<List<AdobeSignAgreement>> GetPendingByOnboardingIdAsync(long onboardingId)
        {
            return await db.Queryable<AdobeSignAgreement>()
                .Where(a => a.OnboardingId == onboardingId && a.Status == "Awaiting" && a.IsValid == true)
                .OrderByDescending(a => a.CreateDate)
                .ToListAsync();
        }
    }
}
