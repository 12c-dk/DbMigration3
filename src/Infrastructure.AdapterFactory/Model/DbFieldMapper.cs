using DbMigration.Domain.Model;

namespace Infrastructure.AdapterFactory.Model
{
    public static class DbFieldMapper
    {
        public static DbFieldDto ToDto(DbField domain)
        {
            return new DbFieldDto
            {
                Name = domain.Name,
                InternalName = domain.InternalName,
                FieldType = domain.FieldType,
                Length = domain.Length,
                IsPrimaryKey = domain.IsPrimaryKey,
                TargetDbColumnDefault = domain.TargetDbColumnDefault,
                IsIdentity = domain.IsIdentity
            };
        }

        public static DbField ToDomain(DbFieldDto dto)
        {
            return new DbField
            {
                Name = dto.Name,
                InternalName = dto.InternalName,
                FieldType = dto.FieldType,
                Length = dto.Length,
                IsPrimaryKey = dto.IsPrimaryKey,
                TargetDbColumnDefault = dto.TargetDbColumnDefault,
                IsIdentity = dto.IsIdentity
            };
        }
    }
}
