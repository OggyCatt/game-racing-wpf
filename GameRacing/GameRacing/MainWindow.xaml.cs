using System;
using System.Collections.Generic;
using System.Data.SqlTypes;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading; // nhập không gian tên threading đầu tiên, để có thể sử dụng dispatcher timer trong mã C#
namespace GameRacing
{
    /// <summary>
    /// Logic tương tác cho MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        DispatcherTimer gameTimer = new DispatcherTimer(); // tạo một đối tượng mới của dispatcher timer có tên là gameTimer
        List<Rectangle> itemRemover = new List<Rectangle>(); // tạo một danh sách mới có tên itemRemover, danh sách này sẽ được sử dụng để xóa bất kỳ hình chữ nhật không sử dụng nào trong trò chơi
        Random rand = new Random(); // tạo một đối tượng mới của lớp Random có tên là rand
        ImageBrush playerImage = new ImageBrush(); // tạo một ImageBrush mới cho người chơi
        ImageBrush starImage = new ImageBrush(); // tạo một ImageBrush mới cho ngôi sao
        Rect playerHitBox; // đối tượng rect này sẽ được sử dụng để tính toán khu vực va chạm của người chơi với các đối tượng khác
        // thiết lập các giá trị cho trò chơi, bao gồm tốc độ cho các xe cản trở và vạch đường, tốc độ của người chơi, số lượng xe, bộ đếm ngôi sao và bộ đếm chế độ PowerMode
        int speed = 15;
        int playerSpeed = 10;
        int carNum;
        int starCounter = 30;
        int powerModeCounter = 200;
        // tạo hai biến double, một cho điểm số và một cho i, biến này sẽ được sử dụng để anim hóa xe người chơi khi chế độ power mode được kích hoạt
        double score;
        double i;
        // chúng ta sẽ cần 4 biến boolean cho trò chơi này, vì tất cả đều là false khi bắt đầu, nên chúng ta sẽ khai báo chúng trong một dòng
        bool moveLeft, moveRight, gameOver, powerMode;
        public MainWindow()
        {
            InitializeComponent();
            myCanvas.Focus(); // thiết lập sự kiện focus của chương trình cho phần tử myCanvas, với dòng này nó sẽ không đăng ký các sự kiện bàn phím
            gameTimer.Tick += GameLoop; // liên kết sự kiện game timer với sự kiện game loop
            gameTimer.Interval = TimeSpan.FromMilliseconds(20); // bộ đếm thời gian này sẽ chạy mỗi 20 mili giây
            StartGame(); // gọi hàm bắt đầu trò chơi
        }
        private void GameLoop(object sender, EventArgs e)
        {
            score += .05; // tăng điểm số thêm 0.05 mỗi lần đồng hồ đếm giờ "tick"
            starCounter -= 1; // giảm 1 từ bộ đếm ngôi sao mỗi lần đồng hồ đếm giờ "tick"
            scoreText.Content = "Survived " + score.ToString("#.#") + " Seconds"; // dòng này sẽ hiển thị số giây đã trôi qua dưới dạng số thập phân trên nhãn điểm số
            playerHitBox = new Rect(Canvas.GetLeft(player), Canvas.GetTop(player), player.Width, player.Height); // gán khu vực va chạm của người chơi
            // dưới đây là hai câu lệnh if kiểm tra xem người chơi có thể di chuyển sang trái hoặc phải trong màn hình không
            if (moveLeft == true && Canvas.GetLeft(player) > 0)
            {
                Canvas.SetLeft(player, Canvas.GetLeft(player) - playerSpeed);
            }
            if (moveRight == true && Canvas.GetLeft(player) + 90 < Application.Current.MainWindow.Width)
            {
                Canvas.SetLeft(player, Canvas.GetLeft(player) + playerSpeed);
            }
            // nếu bộ đếm ngôi sao nhỏ hơn 1, chúng ta gọi hàm tạo ngôi sao và đồng thời tạo một số ngẫu nhiên cho bộ đếm ngôi sao
            if (starCounter < 1)
            {
                MakeStar();
                starCounter = rand.Next(600, 900);
            }
            // dưới đây là vòng lặp chính của trò chơi, trong vòng lặp này chúng ta sẽ lặp qua tất cả các hình chữ nhật có sẵn trong trò chơi
            foreach (var x in myCanvas.Children.OfType<Rectangle>())
            {
                // đầu tiên chúng ta tìm kiếm tất cả các hình chữ nhật trong trò chơi
                // sau đó chúng ta kiểm tra nếu có hình chữ nhật nào có tag là "roadMarks"
                if ((string)x.Tag == "roadMarks")
                {
                    // nếu tìm thấy hình chữ nhật có tag là "roadMarks", ta sẽ di chuyển nó xuống theo tốc độ
                    Canvas.SetTop(x, Canvas.GetTop(x) + speed); // di chuyển nó xuống với tốc độ
                    // nếu vạch đường ra khỏi màn hình thì di chuyển nó lên lại trên cùng màn hình
                    if (Canvas.GetTop(x) > 510)
                    {
                        Canvas.SetTop(x, -152);
                    }
                } // kết thúc kiểm tra vạch đường
                // nếu tìm thấy một hình chữ nhật có tag là "Car"
                if ((string)x.Tag == "Car")
                {
                    Canvas.SetTop(x, Canvas.GetTop(x) + speed); // di chuyển xe xuống theo tốc độ
                    // nếu xe ra khỏi màn hình thì gọi hàm ChangeCars với hình chữ nhật xe hiện tại
                    if (Canvas.GetTop(x) > 500)
                    {
                        ChangeCars(x);
                    }
                    // tạo một đối tượng rect gọi là carHitBox và gán nó cho hình chữ nhật xe
                    Rect carHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);
                    // nếu khu vực va chạm của người chơi và khu vực va chạm của xe giao nhau và chế độ power mode đang bật
                    if (playerHitBox.IntersectsWith(carHitBox) && powerMode == true)
                    {
                        // gọi hàm ChangeCars với hình chữ nhật xe hiện tại
                        ChangeCars(x);
                    }
                    else if (playerHitBox.IntersectsWith(carHitBox) && powerMode == false)
                    {
                        // nếu chế độ power mode tắt và người chơi va chạm với xe thì
                        gameTimer.Stop(); // dừng bộ đếm thời gian
                        scoreText.Content += " Press Enter to replay"; // thêm dòng này vào nhãn điểm số
                        gameOver = true; // đặt biến gameOver thành true
                    }
                } // kết thúc kiểm tra xe
                // nếu tìm thấy một hình chữ nhật có tag là "star"
                if ((string)x.Tag == "star")
                {
                    // di chuyển ngôi sao xuống màn hình mỗi lần 5 pixel
                    Canvas.SetTop(x, Canvas.GetTop(x) + 5);
                    // tạo một đối tượng rect cho ngôi sao và truyền giá trị của ngôi sao vào đó
                    Rect starHitBox = new Rect(Canvas.GetLeft(x), Canvas.GetTop(x), x.Width, x.Height);
                    // nếu khu vực va chạm của người chơi và ngôi sao giao nhau thì
                    if (playerHitBox.IntersectsWith(starHitBox))
                    {
                        // thêm ngôi sao vào danh sách itemRemover
                        itemRemover.Add(x);
                        // bật chế độ power mode
                        powerMode = true;
                        // thiết lập bộ đếm power mode thành 200
                        powerModeCounter = 200;
                    }
                    // nếu ngôi sao ra khỏi màn hình (vượt quá 400 pixels) thì thêm vào danh sách itemRemover
                    if (Canvas.GetTop(x) > 400)
                    {
                        itemRemover.Add(x);
                    }
                } // kết thúc kiểm tra ngôi sao
            } // kết thúc vòng lặp foreach
            // nếu chế độ power mode đang bật
            if (powerMode == true)
            {
                powerModeCounter -= 1; // giảm 1 từ bộ đếm power mode
                // gọi hàm power up
                PowerUp();
                // nếu bộ đếm power mode nhỏ hơn 1 thì
                if (powerModeCounter < 1)
                {
                    // tắt chế độ power mode
                    powerMode = false;
                }
            }
            else
            {
                // nếu chế độ power mode tắt thì đổi hình ảnh xe người chơi về mặc định và đổi nền thành màu xám
                playerImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/playerImage.png"));
                myCanvas.Background = Brushes.Gray;
            }
            // với mỗi đối tượng trong danh sách itemRemover, chúng ta sẽ xóa nó khỏi canvas
            foreach (Rectangle y in itemRemover)
            {
                myCanvas.Children.Remove(y);
            }
            // dưới đây là cấu hình điểm số và tốc độ cho trò chơi
            // khi người chơi tiến bộ trong trò chơi, điểm số sẽ tăng lên và tốc độ xe cản trở sẽ tăng
            if (score >= 10 && score < 20)
            {
                speed = 12;
            }
            if (score >= 20 && score < 30)
            {
                speed = 14;
            }
            if (score >= 30 && score < 40)
            {
                speed = 16;
            }
            if (score >= 40 && score < 50)
            {
                speed = 18;
            }
            if (score >= 50 && score < 80)
            {
                speed = 22;
            }
        }
        private void OnKeyDown(object sender, KeyEventArgs e)
        {
            // sự kiện key down sẽ lắng nghe khi người dùng nhấn phím trái hoặc phải và thay đổi giá trị boolean tương ứng thành true
            if (e.Key == Key.Left)
            {
                moveLeft = true;
            }
            if (e.Key == Key.Right)
            {
                moveRight = true;
            }
        }
        private void OnKeyUP(object sender, KeyEventArgs e)
        {
            // khi người chơi thả phím trái hoặc phải, giá trị boolean tương ứng sẽ được đặt thành false
            if (e.Key == Key.Left)
            {
                moveLeft = false;
            }
            if (e.Key == Key.Right)
            {
                moveRight = false;
            }
            // trong trường hợp này chúng ta sẽ lắng nghe phím enter, nhưng để sự kiện này xảy ra, biến gameOver phải là true
            if (e.Key == Key.Enter && gameOver == true)
            {
                // nếu cả hai điều kiện đều đúng, chúng ta sẽ gọi hàm bắt đầu trò chơi
                StartGame();
            }
        }
        private void StartGame()
        {
            // hàm bắt đầu trò chơi, hàm này sẽ reset tất cả các giá trị về trạng thái mặc định và bắt đầu trò chơi
            speed = 8; // thiết lập tốc độ bằng 8
            gameTimer.Start(); // bắt đầu bộ đếm thời gian
            // thiết lập tất cả các boolean thành false
            moveLeft = false;
            moveRight = false;
            gameOver = false;
            powerMode = false;
            // thiết lập điểm số bằng 0
            score = 0;
            // thiết lập nội dung nhãn điểm số về mặc định
            scoreText.Content = "Survived: 0 Seconds";
            // thiết lập hình ảnh người chơi và hình ảnh ngôi sao từ thư mục hình ảnh
            playerImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/playerImage.png"));
            starImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/star.png"));
            // gán hình ảnh người chơi cho hình chữ nhật player từ canvas
            player.Fill = playerImage;
            // thiết lập màu nền mặc định cho canvas thành màu xám
            myCanvas.Background = Brushes.Gray;
            // chạy một vòng lặp foreach ban đầu để thiết lập các xe và xóa bất kỳ ngôi sao nào trong trò chơi
            foreach (var x in myCanvas.Children.OfType<Rectangle>())
            {
                // nếu tìm thấy hình chữ nhật có tag là "Car" thì
                if ((string)x.Tag == "Car")
                {
                    // thiết lập vị trí ngẫu nhiên cho top và left
                    Canvas.SetTop(x, (rand.Next(100, 400) * -1));
                    Canvas.SetLeft(x, rand.Next(0, 430));
                    // gọi hàm ChangeCars
                    ChangeCars(x);
                }
                // nếu tìm thấy một ngôi sao ngay từ đầu trò chơi, ta sẽ thêm nó vào danh sách itemRemover
                if ((string)x.Tag == "star")
                {
                    itemRemover.Add(x);
                }
            }
            // xóa mọi mục trong danh sách itemRemover tại thời điểm bắt đầu trò chơi
            itemRemover.Clear();
        }
        private void ChangeCars(Rectangle car)
        {
            // chúng ta muốn trò chơi thay đổi hình ảnh của xe cản trở khi chúng ra khỏi màn hình và quay lại
            carNum = rand.Next(1, 6); // để bắt đầu, chúng ta tạo một số ngẫu nhiên từ 1 đến 6
            ImageBrush carImage = new ImageBrush(); // tạo một ImageBrush mới cho hình ảnh xe
            // câu lệnh switch dưới đây sẽ xem số ngẫu nhiên được sinh ra cho carNum và
            // dựa trên số đó sẽ gán hình ảnh khác nhau cho xe
            switch (carNum)
            {
                case 1:
                    carImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/car1.png"));
                    break;
                case 2:
                    carImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/car2.png"));
                    break;
                case 3:
                    carImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/car3.png"));
                    break;
                case 4:
                    carImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/car4.png"));
                    break;
                case 5:
                    carImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/car5.png"));
                    break;
                case 6:
                    carImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/car6.png"));
                    break;
            }
            car.Fill = carImage; // gán hình ảnh đã chọn cho hình chữ nhật xe
            // thiết lập vị trí ngẫu nhiên cho top và left của xe
            Canvas.SetTop(car, (rand.Next(100, 400) * -1));
            Canvas.SetLeft(car, rand.Next(0, 430));
        }
        private void PowerUp()
        {
            // đây là hàm power up, hàm này sẽ chạy khi người chơi thu thập ngôi sao trong trò chơi
            i += .5; // tăng i lên 0.5
            // nếu i lớn hơn 4, ta sẽ đặt lại i về 1
            if (i > 4)
            {
                i = 1;
            }
            // với mỗi lần tăng i, ta sẽ thay đổi hình ảnh xe người chơi thành một trong bốn hình ảnh dưới đây
            switch (i)
            {
                case 1:
                    playerImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/powermode1.png"));
                    break;
                case 2:
                    playerImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/powermode2.png"));
                    break;
                case 3:
                    playerImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/powermode3.png"));
                    break;
                case 4:
                    playerImage.ImageSource = new BitmapImage(new Uri("pack://application:,,,/images/powermode4.png"));
                    break;
            }
            // thay đổi nền thành màu light coral
            myCanvas.Background = Brushes.LightCoral;
        }
        private void MakeStar()
        {
            // đây là hàm tạo ngôi sao
            // hàm này sẽ tạo một hình chữ nhật, gán hình ảnh ngôi sao cho nó và đặt nó lên canvas
            // tạo một hình chữ nhật mới cho ngôi sao với các thuộc tính riêng
            Rectangle newStar = new Rectangle
            {
                Height = 50,
                Width = 50,
                Tag = "star",
                Fill = starImage
            };
            // thiết lập vị trí ngẫu nhiên cho left và top của ngôi sao
            Canvas.SetLeft(newStar, rand.Next(0, 430));
            Canvas.SetTop(newStar, (rand.Next(100, 400) * -1));
            // cuối cùng thêm ngôi sao mới vào canvas để nó có thể được anim hóa và tương tác với người chơi
            myCanvas.Children.Add(newStar);
        }
    }
}
