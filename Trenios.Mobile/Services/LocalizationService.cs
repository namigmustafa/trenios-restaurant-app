using System.ComponentModel;

namespace Trenios.Mobile.Services;

public class LocalizationService : INotifyPropertyChanged
{
    private const string LanguageKey = "app_language";
    private string _currentLanguage = "en";

    public event PropertyChangedEventHandler? PropertyChanged;
    public event Action? OnLanguageChanged;

    public static LocalizationService Instance { get; } = new();

    public string CurrentLanguage
    {
        get => _currentLanguage;
        private set
        {
            if (_currentLanguage != value)
            {
                _currentLanguage = value;
                // Notify all properties changed (null means all properties)
                // This ensures all translation bindings update
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(null));
                OnLanguageChanged?.Invoke();
            }
        }
    }

    public string CurrentLanguageName => CurrentLanguage switch
    {
        "en" => "English",
        "az" => "Azerbaijani",
        "ru" => "Russian",
        "tr" => "Turkish",
        "lv" => "Latvian",
        _ => "English"
    };

    public List<(string Code, string Name)> AvailableLanguages => new()
    {
        ("en", "English"),
        ("az", "Azərbaycan"),
        ("ru", "Русский"),
        ("tr", "Türkçe"),
        ("lv", "Latviešu")
    };

    public LocalizationService()
    {
        // Load saved language
        _currentLanguage = Preferences.Get(LanguageKey, "en");
    }

    public void SetLanguage(string languageCode)
    {
        if (Translations.ContainsKey(languageCode))
        {
            CurrentLanguage = languageCode;
            Preferences.Set(LanguageKey, languageCode);
        }
    }

    public string this[string key] => Get(key);

    public string Get(string key)
    {
        if (Translations.TryGetValue(CurrentLanguage, out var langDict))
        {
            if (langDict.TryGetValue(key, out var value))
                return value;
        }

        // Fallback to English
        if (Translations.TryGetValue("en", out var enDict))
        {
            if (enDict.TryGetValue(key, out var value))
                return value;
        }

        return key;
    }

    private static readonly Dictionary<string, Dictionary<string, string>> Translations = new()
    {
        ["en"] = new()
        {
            // Header
            ["Orders"] = "Orders",
            ["Kitchen"] = "Kitchen",
            ["Logout"] = "Logout",

            // POS
            ["NewOrder"] = "New Order",
            ["CreateOrder"] = "Create Order",
            ["Cancel"] = "Cancel",
            ["Repeat"] = "Repeat",
            ["Total"] = "Total",
            ["Items"] = "items",
            ["NoItemsYet"] = "No items yet",
            ["TapProductToAdd"] = "Tap a product to add",
            ["CartIsEmpty"] = "Cart is empty",
            ["AddProductsToStart"] = "Add products to get started",
            ["CurrentOrder"] = "Current Order",
            ["Quantity"] = "Quantity",
            ["AddToOrder"] = "Add to Order",
            ["Remove"] = "Remove",
            ["ItemTotal"] = "Item Total",
            ["Loading"] = "Loading...",
            ["NoItemsInCategory"] = "No items in this category",
            ["Retry"] = "Retry",

            // Order Creation
            ["CreatingOrder"] = "Creating Order...",
            ["CompleteOrder"] = "Complete Order",
            ["SubmitOrder"] = "Submit order?",
            ["Submit"] = "Submit",
            ["OrderSubmitted"] = "Order Submitted",
            ["Error"] = "Error",
            ["FailedToSubmit"] = "Failed to submit order",

            // Kitchen
            ["KitchenDisplay"] = "Kitchen Display",
            ["Live"] = "Live",
            ["Refresh"] = "Refresh",
            ["Back"] = "Back",
            ["NoActiveOrders"] = "No active orders",
            ["NewOrdersAppear"] = "New orders will appear here automatically",
            ["StartPreparing"] = "Start Preparing",
            ["MarkCompleted"] = "Mark Completed",
            ["Close"] = "Close",

            // Order Status
            ["Created"] = "Created",
            ["Confirmed"] = "Confirmed",
            ["Preparing"] = "Preparing",
            ["Completed"] = "Completed",
            ["Cancelled"] = "Cancelled",

            // Order Types
            ["DineIn"] = "Dine In",
            ["TakeAway"] = "Take Away",
            ["Delivery"] = "Delivery",

            // Auth
            ["Login"] = "Login",
            ["Username"] = "Username",
            ["Password"] = "Password",
            ["EnterUsername"] = "Enter your username",
            ["EnterPassword"] = "Enter your password",
            ["AreYouSureLogout"] = "Are you sure you want to logout?",
            ["Yes"] = "Yes",
            ["No"] = "No",

            // Selection
            ["SelectBranch"] = "Select Branch",
            ["SelectRestaurant"] = "Select Restaurant",
            ["NoBranchesFound"] = "No branches found",
            ["NoRestaurantsFound"] = "No restaurants found",
            ["Active"] = "Active",
            ["Welcome"] = "Welcome",

            // Misc
            ["OK"] = "OK",
            ["Language"] = "Language",
            ["All"] = "All",
            ["NoOrdersFound"] = "No orders found",
            ["Type"] = "Type",
            ["ItemsLabel"] = "Items",
            ["Subtotal"] = "Subtotal",
            ["Tax"] = "Tax",
            ["Discount"] = "Discount",
            ["CancelOrder"] = "Cancel Order",
            ["Complete"] = "Complete"
        },

        ["az"] = new()
        {
            // Header
            ["Orders"] = "Sifarişlər",
            ["Kitchen"] = "Mətbəx",
            ["Logout"] = "Çıxış",

            // POS
            ["NewOrder"] = "Yeni Sifariş",
            ["CreateOrder"] = "Sifariş Yarat",
            ["Cancel"] = "Ləğv et",
            ["Repeat"] = "Təkrarla",
            ["Total"] = "Cəmi",
            ["Items"] = "məhsul",
            ["NoItemsYet"] = "Hələ məhsul yoxdur",
            ["TapProductToAdd"] = "Əlavə etmək üçün məhsula toxunun",
            ["CartIsEmpty"] = "Səbət boşdur",
            ["AddProductsToStart"] = "Başlamaq üçün məhsul əlavə edin",
            ["CurrentOrder"] = "Cari Sifariş",
            ["Quantity"] = "Miqdar",
            ["AddToOrder"] = "Sifarişə Əlavə Et",
            ["Remove"] = "Sil",
            ["ItemTotal"] = "Məhsul Cəmi",
            ["Loading"] = "Yüklənir...",
            ["NoItemsInCategory"] = "Bu kateqoriyada məhsul yoxdur",
            ["Retry"] = "Yenidən cəhd et",

            // Order Creation
            ["CreatingOrder"] = "Sifariş yaradılır...",
            ["CompleteOrder"] = "Sifarişi Tamamla",
            ["SubmitOrder"] = "Sifariş göndərilsin?",
            ["Submit"] = "Göndər",
            ["OrderSubmitted"] = "Sifariş Göndərildi",
            ["Error"] = "Xəta",
            ["FailedToSubmit"] = "Sifariş göndərilə bilmədi",

            // Kitchen
            ["KitchenDisplay"] = "Mətbəx Ekranı",
            ["Live"] = "Canlı",
            ["Refresh"] = "Yenilə",
            ["Back"] = "Geri",
            ["NoActiveOrders"] = "Aktiv sifariş yoxdur",
            ["NewOrdersAppear"] = "Yeni sifarişlər avtomatik görünəcək",
            ["StartPreparing"] = "Hazırlamağa Başla",
            ["MarkCompleted"] = "Tamamlandı",
            ["Close"] = "Bağla",

            // Order Status
            ["Created"] = "Yaradıldı",
            ["Confirmed"] = "Təsdiqləndi",
            ["Preparing"] = "Hazırlanır",
            ["Completed"] = "Tamamlandı",
            ["Cancelled"] = "Ləğv edildi",

            // Order Types
            ["DineIn"] = "Restoranda",
            ["TakeAway"] = "Aparmaq",
            ["Delivery"] = "Çatdırılma",

            // Auth
            ["Login"] = "Daxil ol",
            ["Username"] = "İstifadəçi adı",
            ["Password"] = "Şifrə",
            ["EnterUsername"] = "İstifadəçi adınızı daxil edin",
            ["EnterPassword"] = "Şifrənizi daxil edin",
            ["AreYouSureLogout"] = "Çıxış etmək istədiyinizə əminsiniz?",
            ["Yes"] = "Bəli",
            ["No"] = "Xeyr",

            // Selection
            ["SelectBranch"] = "Filial Seçin",
            ["SelectRestaurant"] = "Restoran Seçin",
            ["NoBranchesFound"] = "Filial tapılmadı",
            ["NoRestaurantsFound"] = "Restoran tapılmadı",
            ["Active"] = "Aktiv",
            ["Welcome"] = "Xoş gəldiniz",

            // Misc
            ["OK"] = "OK",
            ["Language"] = "Dil",
            ["All"] = "Hamısı",
            ["NoOrdersFound"] = "Sifariş tapılmadı",
            ["Type"] = "Növ",
            ["ItemsLabel"] = "Məhsullar",
            ["Subtotal"] = "Ara cəm",
            ["Tax"] = "Vergi",
            ["Discount"] = "Endirim",
            ["CancelOrder"] = "Sifarişi Ləğv Et",
            ["Complete"] = "Tamamla"
        },

        ["ru"] = new()
        {
            // Header
            ["Orders"] = "Заказы",
            ["Kitchen"] = "Кухня",
            ["Logout"] = "Выйти",

            // POS
            ["NewOrder"] = "Новый Заказ",
            ["CreateOrder"] = "Создать Заказ",
            ["Cancel"] = "Отмена",
            ["Repeat"] = "Повторить",
            ["Total"] = "Итого",
            ["Items"] = "товаров",
            ["NoItemsYet"] = "Пока нет товаров",
            ["TapProductToAdd"] = "Нажмите на товар, чтобы добавить",
            ["CartIsEmpty"] = "Корзина пуста",
            ["AddProductsToStart"] = "Добавьте товары для начала",
            ["CurrentOrder"] = "Текущий Заказ",
            ["Quantity"] = "Количество",
            ["AddToOrder"] = "Добавить в Заказ",
            ["Remove"] = "Удалить",
            ["ItemTotal"] = "Сумма товара",
            ["Loading"] = "Загрузка...",
            ["NoItemsInCategory"] = "В этой категории нет товаров",
            ["Retry"] = "Повторить",

            // Order Creation
            ["CreatingOrder"] = "Создание заказа...",
            ["CompleteOrder"] = "Завершить Заказ",
            ["SubmitOrder"] = "Отправить заказ?",
            ["Submit"] = "Отправить",
            ["OrderSubmitted"] = "Заказ Отправлен",
            ["Error"] = "Ошибка",
            ["FailedToSubmit"] = "Не удалось отправить заказ",

            // Kitchen
            ["KitchenDisplay"] = "Экран Кухни",
            ["Live"] = "Онлайн",
            ["Refresh"] = "Обновить",
            ["Back"] = "Назад",
            ["NoActiveOrders"] = "Нет активных заказов",
            ["NewOrdersAppear"] = "Новые заказы появятся автоматически",
            ["StartPreparing"] = "Начать Готовить",
            ["MarkCompleted"] = "Завершить",
            ["Close"] = "Закрыть",

            // Order Status
            ["Created"] = "Создан",
            ["Confirmed"] = "Подтверждён",
            ["Preparing"] = "Готовится",
            ["Completed"] = "Завершён",
            ["Cancelled"] = "Отменён",

            // Order Types
            ["DineIn"] = "В зале",
            ["TakeAway"] = "С собой",
            ["Delivery"] = "Доставка",

            // Auth
            ["Login"] = "Войти",
            ["Username"] = "Имя пользователя",
            ["Password"] = "Пароль",
            ["EnterUsername"] = "Введите имя пользователя",
            ["EnterPassword"] = "Введите пароль",
            ["AreYouSureLogout"] = "Вы уверены, что хотите выйти?",
            ["Yes"] = "Да",
            ["No"] = "Нет",

            // Selection
            ["SelectBranch"] = "Выберите Филиал",
            ["SelectRestaurant"] = "Выберите Ресторан",
            ["NoBranchesFound"] = "Филиалы не найдены",
            ["NoRestaurantsFound"] = "Рестораны не найдены",
            ["Active"] = "Активен",
            ["Welcome"] = "Добро пожаловать",

            // Misc
            ["OK"] = "OK",
            ["Language"] = "Язык",
            ["All"] = "Все",
            ["NoOrdersFound"] = "Заказы не найдены",
            ["Type"] = "Тип",
            ["ItemsLabel"] = "Товары",
            ["Subtotal"] = "Подитог",
            ["Tax"] = "Налог",
            ["Discount"] = "Скидка",
            ["CancelOrder"] = "Отменить Заказ",
            ["Complete"] = "Завершить"
        },

        ["tr"] = new()
        {
            // Header
            ["Orders"] = "Siparişler",
            ["Kitchen"] = "Mutfak",
            ["Logout"] = "Çıkış",

            // POS
            ["NewOrder"] = "Yeni Sipariş",
            ["CreateOrder"] = "Sipariş Oluştur",
            ["Cancel"] = "İptal",
            ["Repeat"] = "Tekrarla",
            ["Total"] = "Toplam",
            ["Items"] = "ürün",
            ["NoItemsYet"] = "Henüz ürün yok",
            ["TapProductToAdd"] = "Eklemek için ürüne dokunun",
            ["CartIsEmpty"] = "Sepet boş",
            ["AddProductsToStart"] = "Başlamak için ürün ekleyin",
            ["CurrentOrder"] = "Mevcut Sipariş",
            ["Quantity"] = "Miktar",
            ["AddToOrder"] = "Siparişe Ekle",
            ["Remove"] = "Kaldır",
            ["ItemTotal"] = "Ürün Toplamı",
            ["Loading"] = "Yükleniyor...",
            ["NoItemsInCategory"] = "Bu kategoride ürün yok",
            ["Retry"] = "Tekrar dene",

            // Order Creation
            ["CreatingOrder"] = "Sipariş oluşturuluyor...",
            ["CompleteOrder"] = "Siparişi Tamamla",
            ["SubmitOrder"] = "Sipariş gönderilsin mi?",
            ["Submit"] = "Gönder",
            ["OrderSubmitted"] = "Sipariş Gönderildi",
            ["Error"] = "Hata",
            ["FailedToSubmit"] = "Sipariş gönderilemedi",

            // Kitchen
            ["KitchenDisplay"] = "Mutfak Ekranı",
            ["Live"] = "Canlı",
            ["Refresh"] = "Yenile",
            ["Back"] = "Geri",
            ["NoActiveOrders"] = "Aktif sipariş yok",
            ["NewOrdersAppear"] = "Yeni siparişler otomatik görünecek",
            ["StartPreparing"] = "Hazırlamaya Başla",
            ["MarkCompleted"] = "Tamamlandı",
            ["Close"] = "Kapat",

            // Order Status
            ["Created"] = "Oluşturuldu",
            ["Confirmed"] = "Onaylandı",
            ["Preparing"] = "Hazırlanıyor",
            ["Completed"] = "Tamamlandı",
            ["Cancelled"] = "İptal edildi",

            // Order Types
            ["DineIn"] = "İçeride",
            ["TakeAway"] = "Paket",
            ["Delivery"] = "Teslimat",

            // Auth
            ["Login"] = "Giriş",
            ["Username"] = "Kullanıcı adı",
            ["Password"] = "Şifre",
            ["EnterUsername"] = "Kullanıcı adınızı girin",
            ["EnterPassword"] = "Şifrenizi girin",
            ["AreYouSureLogout"] = "Çıkış yapmak istediğinize emin misiniz?",
            ["Yes"] = "Evet",
            ["No"] = "Hayır",

            // Selection
            ["SelectBranch"] = "Şube Seçin",
            ["SelectRestaurant"] = "Restoran Seçin",
            ["NoBranchesFound"] = "Şube bulunamadı",
            ["NoRestaurantsFound"] = "Restoran bulunamadı",
            ["Active"] = "Aktif",
            ["Welcome"] = "Hoş geldiniz",

            // Misc
            ["OK"] = "Tamam",
            ["Language"] = "Dil",
            ["All"] = "Tümü",
            ["NoOrdersFound"] = "Sipariş bulunamadı",
            ["Type"] = "Tür",
            ["ItemsLabel"] = "Ürünler",
            ["Subtotal"] = "Ara toplam",
            ["Tax"] = "Vergi",
            ["Discount"] = "İndirim",
            ["CancelOrder"] = "Siparişi İptal Et",
            ["Complete"] = "Tamamla"
        },

        ["lv"] = new()
        {
            // Header
            ["Orders"] = "Pasūtījumi",
            ["Kitchen"] = "Virtuve",
            ["Logout"] = "Iziet",

            // POS
            ["NewOrder"] = "Jauns Pasūtījums",
            ["CreateOrder"] = "Izveidot Pasūtījumu",
            ["Cancel"] = "Atcelt",
            ["Repeat"] = "Atkārtot",
            ["Total"] = "Kopā",
            ["Items"] = "preces",
            ["NoItemsYet"] = "Vēl nav preču",
            ["TapProductToAdd"] = "Pieskarieties precei, lai pievienotu",
            ["CartIsEmpty"] = "Grozs ir tukšs",
            ["AddProductsToStart"] = "Pievienojiet preces, lai sāktu",
            ["CurrentOrder"] = "Pašreizējais Pasūtījums",
            ["Quantity"] = "Daudzums",
            ["AddToOrder"] = "Pievienot Pasūtījumam",
            ["Remove"] = "Noņemt",
            ["ItemTotal"] = "Preces Summa",
            ["Loading"] = "Ielādē...",
            ["NoItemsInCategory"] = "Šajā kategorijā nav preču",
            ["Retry"] = "Mēģināt vēlreiz",

            // Order Creation
            ["CreatingOrder"] = "Izveido pasūtījumu...",
            ["CompleteOrder"] = "Pabeigt Pasūtījumu",
            ["SubmitOrder"] = "Iesniegt pasūtījumu?",
            ["Submit"] = "Iesniegt",
            ["OrderSubmitted"] = "Pasūtījums Iesniegts",
            ["Error"] = "Kļūda",
            ["FailedToSubmit"] = "Neizdevās iesniegt pasūtījumu",

            // Kitchen
            ["KitchenDisplay"] = "Virtuves Ekrāns",
            ["Live"] = "Tiešraide",
            ["Refresh"] = "Atsvaidzināt",
            ["Back"] = "Atpakaļ",
            ["NoActiveOrders"] = "Nav aktīvu pasūtījumu",
            ["NewOrdersAppear"] = "Jauni pasūtījumi parādīsies automātiski",
            ["StartPreparing"] = "Sākt Gatavot",
            ["MarkCompleted"] = "Atzīmēt Pabeigtu",
            ["Close"] = "Aizvērt",

            // Order Status
            ["Created"] = "Izveidots",
            ["Confirmed"] = "Apstiprināts",
            ["Preparing"] = "Gatavo",
            ["Completed"] = "Pabeigts",
            ["Cancelled"] = "Atcelts",

            // Order Types
            ["DineIn"] = "Uz vietas",
            ["TakeAway"] = "Līdzi",
            ["Delivery"] = "Piegāde",

            // Auth
            ["Login"] = "Ieiet",
            ["Username"] = "Lietotājvārds",
            ["Password"] = "Parole",
            ["EnterUsername"] = "Ievadiet lietotājvārdu",
            ["EnterPassword"] = "Ievadiet paroli",
            ["AreYouSureLogout"] = "Vai tiešām vēlaties iziet?",
            ["Yes"] = "Jā",
            ["No"] = "Nē",

            // Selection
            ["SelectBranch"] = "Izvēlieties Filiāli",
            ["SelectRestaurant"] = "Izvēlieties Restorānu",
            ["NoBranchesFound"] = "Nav atrasta neviena filiāle",
            ["NoRestaurantsFound"] = "Nav atrasts neviens restorāns",
            ["Active"] = "Aktīvs",
            ["Welcome"] = "Laipni lūdzam",

            // Misc
            ["OK"] = "Labi",
            ["Language"] = "Valoda",
            ["All"] = "Visi",
            ["NoOrdersFound"] = "Nav atrasts neviens pasūtījums",
            ["Type"] = "Tips",
            ["ItemsLabel"] = "Preces",
            ["Subtotal"] = "Starpsumma",
            ["Tax"] = "Nodoklis",
            ["Discount"] = "Atlaide",
            ["CancelOrder"] = "Atcelt Pasūtījumu",
            ["Complete"] = "Pabeigt"
        }
    };
}
