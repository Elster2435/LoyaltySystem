using NpgsqlTypes;

namespace LoyaltySystem.Core.Enums
{
    public enum PromotionTypeEnum
    {
        [PgName("Общая")]
        General,

        [PgName("Персональная")]
        Personal,

        [PgName("Новый клиент")]
        NewCustomer,

        [PgName("День рождения")]
        Birthday,

        [PgName("Возврат клиента")]
        CustomerReturn,

        [PgName("Возврат покупки")]
        PurchaseReturn
    }
}
