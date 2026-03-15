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
            ["InvalidCredentials"] = "Invalid username or password.",
            ["AreYouSureLogout"] = "Are you sure you want to logout?",
            ["Yes"] = "Yes",
            ["No"] = "No",

            // Selection
            ["SelectBranch"] = "Select Branch",
            ["SelectRestaurant"] = "Select Restaurant",
            ["ChooseRestaurant"] = "Choose a restaurant to manage",
            ["ChooseBranch"] = "Select Branch",
            ["Branches"] = "branches",
            ["NoBranchesFound"] = "No branches found",
            ["NoRestaurantsFound"] = "No restaurants found",
            ["Active"] = "Active",
            ["Welcome"] = "Welcome",

            // Table Selection
            ["SelectOrderType"] = "Select Order Type",
            ["SelectTable"] = "Select Table",
            ["TableRequired"] = "Please select a table",
            ["Skip"] = "Skip",
            ["Confirm"] = "Confirm",
            ["Seats"] = "seats",
            ["Table"] = "Table",

            // Tables Page
            ["Tables"] = "Tables",
            ["Reserved"] = "Reserved",
            ["Available"] = "Available",
            ["Checkout"] = "Checkout",
            ["MoveTable"] = "Move Table",
            ["Move"] = "Move",
            ["Release"] = "Release",
            ["Duration"] = "Duration",
            ["TotalAmount"] = "Total Amount",
            ["NoReservation"] = "No active reservation",
            ["ConfirmCheckout"] = "Complete all orders and release this table?",
            ["ConfirmRelease"] = "Cancel all orders and release this table?",
            ["SelectTargetTable"] = "Select target table",
            ["NoTablesFound"] = "No tables found",
            ["TableDetails"] = "Table Details",
            ["StartedAt"] = "Started at",
            ["OrdersInTable"] = "Orders",
            ["CheckoutSuccess"] = "Table checked out successfully",
            ["MoveSuccess"] = "Table moved successfully",
            ["ReleaseSuccess"] = "Table released successfully",
            ["EnterReleaseReason"] = "Enter reason for release:",
            ["ReleasePlaceholder"] = "e.g., Customer left, No payment",

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
            ["Complete"] = "Complete",
            ["RestaurantHome"] = "Restaurant Home",
            ["ShowOrders"] = "Show Orders",
            ["From"] = "From",
            ["To"] = "To",
            ["ShowToday"] = "Show Today Only",
            ["CancelOrderTitle"] = "Cancel Order",
            ["EnterCancellationReason"] = "Please enter cancellation reason:",
            ["CancellationPlaceholder"] = "e.g., Customer request, Out of stock"
        },

        ["az"] = new()
        {
            // Header
            ["Orders"] = "Sifarişlər",
            ["Kitchen"] = "Mətbəx",
            ["Logout"] = "Çıxış",

            // Table Selection
            ["SelectOrderType"] = "Sifariş Növünü Seçin",
            ["SelectTable"] = "Masa Seçin",
            ["TableRequired"] = "Zəhmət olmasa masa seçin",
            ["Skip"] = "Keç",
            ["Confirm"] = "Təsdiqlə",
            ["Seats"] = "nəfərlik",
            ["Table"] = "Masa",

            // Tables Page
            ["Tables"] = "Masalar",
            ["Reserved"] = "Tutulub",
            ["Available"] = "Boşdur",
            ["Checkout"] = "Hesab",
            ["MoveTable"] = "Masa Dəyiş",
            ["Move"] = "Köçür",
            ["Release"] = "Boşalt",
            ["Duration"] = "Müddət",
            ["TotalAmount"] = "Ümumi Məbləğ",
            ["NoReservation"] = "Aktiv rezerv yoxdur",
            ["ConfirmCheckout"] = "Bütün sifarişlər tamamlansın və masa boşaldılsın?",
            ["ConfirmRelease"] = "Bütün sifarişlər ləğv edilsin və masa boşaldılsın?",
            ["SelectTargetTable"] = "Hədəf masanı seçin",
            ["NoTablesFound"] = "Masa tapılmadı",
            ["TableDetails"] = "Masa Detalları",
            ["StartedAt"] = "Başladı",
            ["OrdersInTable"] = "Sifarişlər",
            ["CheckoutSuccess"] = "Masa uğurla hesablandı",
            ["MoveSuccess"] = "Masa uğurla dəyişdirildi",
            ["ReleaseSuccess"] = "Masa uğurla boşaldıldı",
            ["EnterReleaseReason"] = "Boşaltma səbəbini daxil edin:",
            ["ReleasePlaceholder"] = "məs: Müştəri getdi, Ödəniş yoxdur",

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
            ["InvalidCredentials"] = "İstifadəçi adı və ya şifrə yanlışdır.",
            ["AreYouSureLogout"] = "Çıxış etmək istədiyinizə əminsiniz?",
            ["Yes"] = "Bəli",
            ["No"] = "Xeyr",

            // Selection
            ["SelectBranch"] = "Filial Seçin",
            ["SelectRestaurant"] = "Restoran Seçin",
            ["ChooseRestaurant"] = "İdarə etmək üçün restoran seçin",
            ["ChooseBranch"] = "Filial Seçin",
            ["Branches"] = "filial",
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
            ["Complete"] = "Tamamla",
            ["RestaurantHome"] = "Restoran Əsas Səhifə",
            ["ShowOrders"] = "Sifarişləri Göstər",
            ["From"] = "-dan",
            ["To"] = "-dək",
            ["ShowToday"] = "Yalnız Bu Gün",
            ["CancelOrderTitle"] = "Sifarişi Ləğv Et",
            ["EnterCancellationReason"] = "Zəhmət olmasa ləğv səbəbini daxil edin:",
            ["CancellationPlaceholder"] = "məs: Müştəri sorğusu, Stokda yoxdur"
        },

        ["ru"] = new()
        {
            // Header
            ["Orders"] = "Заказы",
            ["Kitchen"] = "Кухня",
            ["Logout"] = "Выйти",

            // Table Selection
            ["SelectOrderType"] = "Выберите тип заказа",
            ["SelectTable"] = "Выберите столик",
            ["TableRequired"] = "Пожалуйста, выберите столик",
            ["Skip"] = "Пропустить",
            ["Confirm"] = "Подтвердить",
            ["Seats"] = "мест",
            ["Table"] = "Столик",

            // Tables Page
            ["Tables"] = "Столики",
            ["Reserved"] = "Занят",
            ["Available"] = "Свободен",
            ["Checkout"] = "Расчёт",
            ["MoveTable"] = "Переместить",
            ["Move"] = "Перенос",
            ["Release"] = "Освободить",
            ["Duration"] = "Длительность",
            ["TotalAmount"] = "Общая сумма",
            ["NoReservation"] = "Нет активной брони",
            ["ConfirmCheckout"] = "Завершить все заказы и освободить столик?",
            ["ConfirmRelease"] = "Отменить все заказы и освободить столик?",
            ["SelectTargetTable"] = "Выберите столик назначения",
            ["NoTablesFound"] = "Столики не найдены",
            ["TableDetails"] = "Детали столика",
            ["StartedAt"] = "Начало",
            ["OrdersInTable"] = "Заказы",
            ["CheckoutSuccess"] = "Столик успешно рассчитан",
            ["MoveSuccess"] = "Столик успешно перемещён",
            ["ReleaseSuccess"] = "Столик успешно освобождён",
            ["EnterReleaseReason"] = "Введите причину освобождения:",
            ["ReleasePlaceholder"] = "напр: Клиент ушёл, Без оплаты",

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
            ["InvalidCredentials"] = "Неверное имя пользователя или пароль.",
            ["AreYouSureLogout"] = "Вы уверены, что хотите выйти?",
            ["Yes"] = "Да",
            ["No"] = "Нет",

            // Selection
            ["SelectBranch"] = "Выберите Филиал",
            ["SelectRestaurant"] = "Выберите Ресторан",
            ["ChooseRestaurant"] = "Выберите ресторан для управления",
            ["ChooseBranch"] = "Выберите Филиал",
            ["Branches"] = "филиалов",
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
            ["Complete"] = "Завершить",
            ["RestaurantHome"] = "Главная Ресторана",
            ["ShowOrders"] = "Показать Заказы",
            ["From"] = "От",
            ["To"] = "До",
            ["ShowToday"] = "Только Сегодня",
            ["CancelOrderTitle"] = "Отменить Заказ",
            ["EnterCancellationReason"] = "Пожалуйста, укажите причину отмены:",
            ["CancellationPlaceholder"] = "напр: Запрос клиента, Нет в наличии"
        },

        ["tr"] = new()
        {
            // Header
            ["Orders"] = "Siparişler",
            ["Kitchen"] = "Mutfak",
            ["Logout"] = "Çıkış",

            // Table Selection
            ["SelectOrderType"] = "Sipariş Türünü Seçin",
            ["SelectTable"] = "Masa Seçin",
            ["TableRequired"] = "Lütfen bir masa seçin",
            ["Skip"] = "Atla",
            ["Confirm"] = "Onayla",
            ["Seats"] = "kişilik",
            ["Table"] = "Masa",

            // Tables Page
            ["Tables"] = "Masalar",
            ["Reserved"] = "Dolu",
            ["Available"] = "Boş",
            ["Checkout"] = "Hesap",
            ["MoveTable"] = "Masa Taşı",
            ["Move"] = "Taşı",
            ["Release"] = "Boşalt",
            ["Duration"] = "Süre",
            ["TotalAmount"] = "Toplam Tutar",
            ["NoReservation"] = "Aktif rezervasyon yok",
            ["ConfirmCheckout"] = "Tüm siparişler tamamlansın ve masa boşaltılsın mı?",
            ["ConfirmRelease"] = "Tüm siparişler iptal edilsin ve masa boşaltılsın mı?",
            ["SelectTargetTable"] = "Hedef masayı seçin",
            ["NoTablesFound"] = "Masa bulunamadı",
            ["TableDetails"] = "Masa Detayları",
            ["StartedAt"] = "Başlangıç",
            ["OrdersInTable"] = "Siparişler",
            ["CheckoutSuccess"] = "Masa başarıyla hesaplandı",
            ["MoveSuccess"] = "Masa başarıyla taşındı",
            ["ReleaseSuccess"] = "Masa başarıyla boşaltıldı",
            ["EnterReleaseReason"] = "Boşaltma nedenini girin:",
            ["ReleasePlaceholder"] = "örn: Müşteri gitti, Ödeme yapılmadı",

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
            ["InvalidCredentials"] = "Kullanıcı adı veya şifre hatalı.",
            ["AreYouSureLogout"] = "Çıkış yapmak istediğinize emin misiniz?",
            ["Yes"] = "Evet",
            ["No"] = "Hayır",

            // Selection
            ["SelectBranch"] = "Şube Seçin",
            ["SelectRestaurant"] = "Restoran Seçin",
            ["ChooseRestaurant"] = "Yönetmek için bir restoran seçin",
            ["ChooseBranch"] = "Şube Seçin",
            ["Branches"] = "şube",
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
            ["Complete"] = "Tamamla",
            ["RestaurantHome"] = "Restoran Ana Sayfa",
            ["ShowOrders"] = "Siparişleri Göster",
            ["From"] = "Başlangıç",
            ["To"] = "Bitiş",
            ["ShowToday"] = "Sadece Bugün",
            ["CancelOrderTitle"] = "Siparişi İptal Et",
            ["EnterCancellationReason"] = "Lütfen iptal nedenini girin:",
            ["CancellationPlaceholder"] = "örn: Müşteri talebi, Stokta yok"
        },

        ["lv"] = new()
        {
            // Header
            ["Orders"] = "Pasūtījumi",
            ["Kitchen"] = "Virtuve",
            ["Logout"] = "Iziet",

            // Table Selection
            ["SelectOrderType"] = "Izvēlieties pasūtījuma veidu",
            ["SelectTable"] = "Izvēlieties galdiņu",
            ["TableRequired"] = "Lūdzu, izvēlieties galdiņu",
            ["Skip"] = "Izlaist",
            ["Confirm"] = "Apstiprināt",
            ["Seats"] = "vietas",
            ["Table"] = "Galdiņš",

            // Tables Page
            ["Tables"] = "Galdiņi",
            ["Reserved"] = "Aizņemts",
            ["Available"] = "Brīvs",
            ["Checkout"] = "Norēķins",
            ["MoveTable"] = "Pārvietot",
            ["Move"] = "Pārvietot",
            ["Release"] = "Atbrīvot",
            ["Duration"] = "Ilgums",
            ["TotalAmount"] = "Kopējā summa",
            ["NoReservation"] = "Nav aktīvas rezervācijas",
            ["ConfirmCheckout"] = "Pabeigt visus pasūtījumus un atbrīvot galdiņu?",
            ["ConfirmRelease"] = "Atcelt visus pasūtījumus un atbrīvot galdiņu?",
            ["SelectTargetTable"] = "Izvēlieties mērķa galdiņu",
            ["NoTablesFound"] = "Nav atrasts neviens galdiņš",
            ["TableDetails"] = "Galdiņa detaļas",
            ["StartedAt"] = "Sākums",
            ["OrdersInTable"] = "Pasūtījumi",
            ["CheckoutSuccess"] = "Galdiņš veiksmīgi norēķināts",
            ["MoveSuccess"] = "Galdiņš veiksmīgi pārvietots",
            ["ReleaseSuccess"] = "Galdiņš veiksmīgi atbrīvots",
            ["EnterReleaseReason"] = "Ievadiet atbrīvošanas iemeslu:",
            ["ReleasePlaceholder"] = "piem: Klients aizgāja, Nav samaksas",

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
            ["InvalidCredentials"] = "Nepareizs lietotājvārds vai parole.",
            ["AreYouSureLogout"] = "Vai tiešām vēlaties iziet?",
            ["Yes"] = "Jā",
            ["No"] = "Nē",

            // Selection
            ["SelectBranch"] = "Izvēlieties Filiāli",
            ["SelectRestaurant"] = "Izvēlieties Restorānu",
            ["ChooseRestaurant"] = "Izvēlieties restorānu pārvaldībai",
            ["ChooseBranch"] = "Izvēlieties Filiāli",
            ["Branches"] = "filiāles",
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
            ["Complete"] = "Pabeigt",
            ["RestaurantHome"] = "Restorāna Sākums",
            ["ShowOrders"] = "Rādīt Pasūtījumus",
            ["From"] = "No",
            ["To"] = "Līdz",
            ["ShowToday"] = "Tikai Šodien",
            ["CancelOrderTitle"] = "Atcelt Pasūtījumu",
            ["EnterCancellationReason"] = "Lūdzu, ievadiet atcelšanas iemeslu:",
            ["CancellationPlaceholder"] = "piem: Klienta pieprasījums, Nav noliktavā"
        }
    };
}
