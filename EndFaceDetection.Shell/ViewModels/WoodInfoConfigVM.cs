using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EndFaceDetection.LogModule;
using EndFaceDetection.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Windows;

namespace EndFaceDetection.Shell.ViewModels
{
    public partial class WoodInfoConfigVM:ObservableObject
    {
        WoodService woodService { get; set; }
        public WoodInfoConfigVM() 
        {
            App app = App._app;
            woodService = app.WoodService;
            Load();
        }



        #region view属性
        [ObservableProperty]
        private ObservableCollection<WoodInfo> woodInfos = new ObservableCollection<WoodInfo>();

        [ObservableProperty]
        private ObservableCollection<string> cameraNames = new ObservableCollection<string> {"C1","C2" };
        [ObservableProperty]
        private int selectedIndex;

        private string? orCode;
        public string? ORCode
        {
            get { return orCode; }
            set { SetProperty(ref orCode, value?.ToUpper()); }
        }

        [ObservableProperty]
        private ushort posion1;

        [ObservableProperty]
        private ushort posion2;

        [ObservableProperty]
        private ushort posion3;
        #endregion

        #region 命令
        [RelayCommand]
        private void Load()
        {

            try
            {
                WoodInfos.Clear();
                foreach(var woodInfo in woodService.GetWoodInfos())
                {
                    WoodInfos.Add(woodInfo);
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        private void Save()
        {
            try
            {
                if (WoodInfos.Count > 0)
                {
                    woodService.SaveWoodInfos(WoodInfos);
                    Load();
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        private void Update()
        {
            try
            {
                if (WoodInfos.Count > 0)
                {
                    WoodInfo woodInfo = WoodInfos.FirstOrDefault((w) => w.ORCode == ORCode);
                    if ((woodInfo != null))
                    {
                        woodInfo.CameraName = CameraNames[SelectedIndex];
                        woodInfo.Positon1 = Posion1;
                        woodInfo.Positon2 = Posion2;
                        woodInfo.Positon3 = Posion3;
                        Save();
                    }
                    else
                    {
                        MessageBox.Show($"二维码：{orCode}，在数据库中不存在");
                        return;
                    }
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        private void Add()
        {
            try
            {
                if(ORCode==null)
                {
                    MessageBox.Show("二维码不能为空！");
                    return;
                }
                if (WoodInfos.FirstOrDefault((w) => w.ORCode == orCode) == null)
                {
                    WoodInfos.Add(new WoodInfo { ORCode = ORCode, CameraName = CameraNames[SelectedIndex], Positon1 = Posion1, Positon2 = Posion2, Positon3 = Posion3 });
                    Save();
                }
                else
                {
                    MessageBox.Show($"此二维码：{orCode}不存在！");
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.ToString()); 
            }
        }

        [RelayCommand]
        private void Select(string or)
        {
            try
            {
                WoodInfo woodInfo = WoodInfos.FirstOrDefault(w => w.ORCode == or);
                if (woodInfo != null)
                {
                    ORCode = woodInfo.ORCode;
                    SelectedIndex = CameraNames.IndexOf(woodInfo.CameraName);
                    Posion1 = woodInfo.Positon1;
                    Posion2 = woodInfo.Positon2;
                    Posion3 = woodInfo.Positon3;
                }
                else
                {
                    Load();
                }
            }
            catch (Exception ex) 
            {
                MessageBox.Show(ex.Message);
            }
        }

        [RelayCommand]
        private void Delete(string or)
        {
            try
            {
                var wood = WoodInfos.FirstOrDefault((wood) => wood.ORCode == or);
                if (wood != null)
                {
                    var result = MessageBox.Show($"是否删除二维码为：{or}的木板型号","确定",MessageBoxButton.YesNo,MessageBoxImage.Question,MessageBoxResult.Yes);
                    if(result==MessageBoxResult.Yes)
                    {
                        WoodInfos.Remove(wood);
                        Save();
                        MessageBox.Show($"删除二维码为：{or}的木板型号");
                    }
                }
                else
                {
                    Load();
                }
            }catch(Exception ex)
            {
                MessageBox.Show(ex.Message); 
            }
        }

        [RelayCommand]
        private void LoadFromFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            // 设置对话框的标题
            openFileDialog.Title = "选择文件";
            openFileDialog.Filter = "Json文件|*.Json";
           if(openFileDialog.ShowDialog()==true)
            {
                string filePath = openFileDialog.FileName;
                WoodInfos.Clear();
                foreach (var wood in woodService.GetWoodInfos(filePath)) 
                {
                    WoodInfos.Add(wood);
                }
            }
        }

        [RelayCommand]
        private void SaveToJson()
        {
            if( WoodInfos.Count == 0 )
            {
                MessageBox.Show("木板型号数据不能为空！");
                return;
            }
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            // 设置对话框的标题
            saveFileDialog.Title = "另存为";
            saveFileDialog.Filter = "Json文件|*.Json";
            if (saveFileDialog.ShowDialog() == true) 
            {
                string json = saveFileDialog.FileName;
                woodService.SaveWoodInfos(WoodInfos,json);
            }
        }
        #endregion
    }
}
