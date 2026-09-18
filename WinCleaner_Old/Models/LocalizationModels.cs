using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using CommunityToolkit.Mvvm.ComponentModel;

namespace WinCleaner.Models
{
    public enum SupportedLanguage
    {
        English,
        Vietnamese,
        ChineseSimplified,
        ChineseTraditional,
        Japanese,
        Korean,
        French,
        German,
        Spanish,
        Russian,
        Portuguese,
        Italian,
        Dutch,
        Polish,
        Turkish,
        Arabic,
        Hebrew,
        Hindi,
        Thai,
        Indonesian
    }

    public class LocalizationResource : ObservableObject
    {
        private string _key = "";
        private string _value = "";
        private string _comment = "";

        public string Key
        {
            get => _key;
            set => SetProperty(ref _key, value);
        }

        public string Value
        {
            get => _value;
            set => SetProperty(ref _value, value);
        }

        public string Comment
        {
            get => _comment;
            set => SetProperty(ref _comment, value);
        }
    }

    public class LanguagePack : ObservableObject
    {
        private SupportedLanguage _language = SupportedLanguage.English;
        private string _displayName = "";
        private string _nativeName = "";
        private string _cultureCode = "";
        private bool _isRtl = false;
        private Dictionary<string, string> _translations = new();
        private DateTime _lastUpdated = DateTime.Now;
        private string _version = "1.0.0";
        private string _author = "";
        private bool _isComplete = false;
        private double _completionPercentage = 0;

        public SupportedLanguage Language
        {
            get => _language;
            set => SetProperty(ref _language, value);
        }

        public string DisplayName
        {
            get => _displayName;
            set => SetProperty(ref _displayName, value);
        }

        public string NativeName
        {
            get => _nativeName;
            set => SetProperty(ref _nativeName, value);
        }

        public string CultureCode
        {
            get => _cultureCode;
            set => SetProperty(ref _cultureCode, value);
        }

        public bool IsRtl
        {
            get => _isRtl;
            set => SetProperty(ref _isRtl, value);
        }

        public Dictionary<string, string> Translations
        {
            get => _translations;
            set => SetProperty(ref _translations, value);
        }

        public DateTime LastUpdated
        {
            get => _lastUpdated;
            set => SetProperty(ref _lastUpdated, value);
        }

        public string Version
        {
            get => _version;
            set => SetProperty(ref _version, value);
        }

        public string Author
        {
            get => _author;
            set => SetProperty(ref _author, value);
        }

        public bool IsComplete
        {
            get => _isComplete;
            set => SetProperty(ref _isComplete, value);
        }

        public double CompletionPercentage
        {
            get => _completionPercentage;
            set => SetProperty(ref _completionPercentage, value);
        }

        public int TranslationCount => _translations.Count;
    }

    public class LocalizationSettings : ObservableObject
    {
        private SupportedLanguage _currentLanguage = SupportedLanguage.English;
        private bool _useSystemLanguage = true;
        private bool _fallbackToEnglish = true;
        private bool _showMissingTranslations = false;
        private string _customLanguagePath = "";

        public SupportedLanguage CurrentLanguage
        {
            get => _currentLanguage;
            set => SetProperty(ref _currentLanguage, value);
        }

        public bool UseSystemLanguage
        {
            get => _useSystemLanguage;
            set => SetProperty(ref _useSystemLanguage, value);
        }

        public bool FallbackToEnglish
        {
            get => _fallbackToEnglish;
            set => SetProperty(ref _fallbackToEnglish, value);
        }

        public bool ShowMissingTranslations
        {
            get => _showMissingTranslations;
            set => SetProperty(ref _showMissingTranslations, value);
        }

        public string CustomLanguagePath
        {
            get => _customLanguagePath;
            set => SetProperty(ref _customLanguagePath, value);
        }
    }
}