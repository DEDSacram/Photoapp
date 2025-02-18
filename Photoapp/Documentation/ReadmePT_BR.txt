--------------- Xamarin forms extensions ----------------

para inicializar seu app coloque em sua App.cs isso 
obs: se estiver usando o App.xaml apague e crie uma classe ;) não seja preguiçoso!

public class App : BootStrapper{
    public static new App Current {get;set;}
    
    public App(){
       NavigationServiceFactory(new NavigationPage(new YourPage()));
    }
    
}
============================================
se estiver usando mvvm todas as suas viewmodels herdarão de ViewModelBase (FormsExtensions.Mvvm)

para adicionar uma navegação crie uma AppViewModelBase que estende de ViewModelBase (FormsExtensions.Mvvm) e 
adicione isso 

public INavigationService NavigationService { get { return App.Current.NavigationService; } }

como navergar emtre pagionas em sua viewmodel:

NavigationService.NavigateAsync(new YourPage(),parametro,bool animado);
NavigationService.NavigateModalAsync(new YourPage(),parametro,bool animated);
NavigationService.GoBack(bool animated);
NavigationService.ModalGoBack(bool animated);
NavigationService.ClearHistory(bool animated);


em sua viewmodelbase possuirá os seguintes metodos sobrescritos

        public virtual async Task OnNavigatedFromAsync() 
        {
          //... carrega a viewmodel quando volta de uma pagina
        }

        public virtual async Task OnNavigatedToAsync(object parameter, NavigationMode mode) 
        {
           object parameter recupera o parametro passado no navigationservice
           
           LoadData((MyObject)parameter);   //... carrega a viewmodel          
        
        }

        public virtual async Task OnNavigatingToAsync(object parameter, NavigationMode mode)
        {
        // garrega em quanto navega
        }
==============================================================        
Styles

   public class DefaultAppStyleKit : StyleKit
    {
        public DefaultAppStyleKit()
        {
            PrimaryColor = Color.FromHex("#9C27B0");
            PrimaryDarkColor = Color.FromHex("#7B1FA2");
            PrimaryLightColor = Color.FromHex("#E1BEE7");
            SecondaryColor = Color.FromHex("#9C27B0");
            SecondaryDarkColor = Color.FromHex("#7B1FA2");
            AccentColor = Color.FromHex("#FF4081");
            PrimaryTextColor = Color.FromHex("#212121");
            SecondaryTextColor = Color.FromHex("#ffffff");
            DividerColor = Color.FromHex("#B6B6B6");
            WindowColor = PrimaryDarkColor;
        }
    }
    
    em sua App.cs coloque isso
    
    public App(){
      StyleKit = new DefaultAppStyleKit();
     ...
     
     acessando o stylekit
     
     var label = new Label(){
        TextColor = App.Current.StyleKit.PrimaryColor
     }
    
===============================================================================
Controles Novos

FloatingActionButtonView somente android
CheckBox android ios
RadioButton android ios -> boa pratica use junto com o BindableRadioGroup
BindableRadioGroup android ios
ImageButton android ios
LabeledEntry android, ios embreve
HyperLinkLabel android ios
BottomTabbedPage android (ios ja é por padrão)
ContentPageBase android ios
TabbedPageBase android ios
TapViewBehavior android ios
ImageGalleryVew android ios
BindableToolbarItem android ios


========================================================================
helpers 
LocationHelper android ios

=========================================================================
extensions 
converte um list,ienumerable etc.. mylist.ToObservableCollection(); android ios

Atençao aalguns controles podem nao funcionar direito no iOs 




