# Sửa chữa

Sửa chữa tập hợp các công cụ có thể khắc phục sự cố. Mỗi công cụ đều chẩn đoán trước. Một sửa chữa chỉ được đề xuất khi bằng chứng mới cho thấy đúng vấn đề mà nó khắc phục, và chỉ chạy sau khi bạn bật **Tự động sửa các vấn đề đã xác nhận** hoặc tự xác nhận một sửa chữa riêng lẻ. Mọi sửa chữa đều được ghi nhật ký và xác minh sau đó.

Hướng dẫn từng bước: [Sửa tệp hệ thống](repair-system-files.html) và [Sửa Windows Update](fix-windows-update.html).

## Sửa chữa tự động và thủ công

**Tự động sửa các vấn đề đã xác nhận** cho phép một công cụ chẩn đoán chạy các sửa chữa mà nó chứng minh là cần thiết sau khi quét. Tùy chọn này tắt theo mặc định.

Với một sửa chữa bảo trì riêng lẻ, mở **Nâng cao: nhật ký và sửa chữa thủ công** và chọn **Sửa chữa**. Chọn **Kho thành phần (DISM)** hoặc **Các tệp hệ thống (SFC)**, đọc bản xem trước, đánh dấu **Chạy sửa chữa này trên máy tính này** và nhấn **Chạy sửa chữa**. Xem [Cách giữ an toàn khi thay đổi](safety.html) để biết điều gì xảy ra trước và sau mọi thay đổi.

## Công cụ trong mục này

Dùng các công cụ này sau khi chẩn đoán xác định được vấn đề. Sửa chữa cần bằng chứng đã xác nhận và sự phê duyệt tường minh.

### Dịch vụ Windows cốt lõi

Kiểm tra cùng lúc các dịch vụ Power, Plug and Play, User Manager, Group Policy Client, Network Store Interface, RPC Endpoint Mapper, Remote Procedure Call, DCOM Server Process Launcher, Background Tasks Infrastructure và CoreMessaging. Khôi phục tùy chọn chỉ khởi động đúng các dịch vụ tự động đang bật mà bằng chứng mới xác nhận đã dừng sau khoảng thời gian khởi động và kiểm tra phụ thuộc đầy đủ. Các dịch vụ khởi động theo yêu cầu được giữ nguyên khi nhàn rỗi. Gói nguồn điện, thiết bị, trình điều khiển, tài khoản, thông tin xác thực, hồ sơ, chính sách, bộ điều hợp, địa chỉ, tuyến đường, DNS, cấu hình tường lửa, đăng ký, điểm cuối, đăng ký tác vụ, dữ liệu ứng dụng, quyền dịch vụ và kiểu khởi động được giữ nguyên. Không có yêu cầu làm mới chính sách, quét thiết bị, thao tác tài khoản, thay đổi mạng, thay đổi cài đặt nguồn điện, thay đổi đăng ký hay khởi chạy tác vụ. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm hoạt động đầu cuối của tính năng.

### Tệp hệ thống

Kiểm kê có giới hạn trạng thái của gói bảo trì, tính năng tùy chọn và khả năng của Windows, kiểm tra điều kiện tiên quyết, quét các tệp hệ thống được bảo vệ và tự động chọn SFC chỉ khi kiểm tra trực tiếp xác nhận có hỏng hóc. Trạng thái chờ của gói, tính năng hoặc khả năng cần được xem lại nhưng không cho phép cài đặt, gỡ bỏ, dọn dẹp, hoàn tác, bật hay tắt.

### Kho WMI

Kiểm tra tính khả dụng của dịch vụ WMI, sau đó là tính nhất quán của kho. Tự động sửa có thể khởi động dịch vụ đã dừng đủ điều kiện và cứu dữ liệu khỏi hỏng hóc đã xác nhận, sau đó xác minh từng kết quả. Cài đặt của dịch vụ bị tắt được giữ nguyên. Các đăng ký đọc được sẽ được hợp nhất; nội dung không đọc được có thể cần cài đặt lại nhà cung cấp. Các ứng dụng khách quản lý có thể bị gián đoạn. Không đặt lại, không xóa và không hoàn tác tự động.

### Thông báo hệ thống Windows

Đọc lịch sử Service Control Manager gần đây, sau đó kiểm tra cùng lúc COM+ Event System và System Event Notification Service. Khôi phục tùy chọn xác minh mức sẵn sàng của COM+ Event System trước khi khởi động SENS. Các thông báo đăng nhập, mạng, nguồn điện và COM+ hiện có có thể tiếp tục theo cấu hình hiện tại. Danh mục COM+, đăng ký nhận, đăng ký thông báo, bên tiêu thụ, tài khoản, quyền, cài đặt khôi phục, chính sách và kiểu khởi động được giữ nguyên. Không có đăng ký nhận, đăng ký, bên tiêu thụ, tài khoản, quyền hay chính sách nào bị thay đổi. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm việc gửi thông báo đầu cuối.

### Dịch vụ chính sách chẩn đoán

Xem lại lịch sử hoạt động có giới hạn của Diagnostic Policy, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của dịch vụ. Bằng chứng chưa đầy đủ được ưu tiên. Bản ghi trong quá khứ không thể cho phép khôi phục dịch vụ hay thao tác khắc phục sự cố. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các kịch bản chẩn đoán hiện có có thể tiếp tục. Chính sách, dữ liệu khắc phục sự cố và kiểu khởi động được giữ nguyên. Không có trình khắc phục sự cố hay bản sửa được đề xuất nào được chạy. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm phát hiện lỗi, hoàn tất chẩn đoán hay kết quả sửa chữa.

### Báo cáo lỗi Windows

Xem lại cấu hình kết xuất sự cố, siêu dữ liệu tệp kết xuất có giới hạn, báo cáo sự cố hệ thống gần đây, lịch sử lỗi chính xác của dịch vụ Windows Error Reporting và mức sẵn sàng hiện tại của dịch vụ. Báo cáo trong quá khứ không xác lập lỗi hiện tại hay nguyên nhân gốc và không thể cho phép khôi phục. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các báo cáo đang chờ trong hàng đợi có thể tiếp tục theo chính sách hiện tại. Kho lưu báo cáo, tệp kết xuất, sự đồng ý, chính sách tải lên và kiểu khởi động được giữ nguyên. Không có yêu cầu tải lên hay xóa. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm việc tạo, tải lên, gửi báo cáo, giải quyết vấn đề hay chẩn đoán sự cố.

### Bộ thu thập sự kiện Windows

Xem lại danh sách đăng ký nhận có giới hạn của Windows Event Collector, lịch sử lỗi chính xác của dịch vụ và mức sẵn sàng hiện tại của dịch vụ. Danh sách trống có thể là có chủ ý, và số lượng đăng ký nhận hay lỗi trong quá khứ không thể cho phép khôi phục. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các đăng ký nhận hiện có có thể tiếp tục thu thập sự kiện từ xa. Đăng ký nhận, sự kiện được chuyển tiếp, thông tin xác thực, cài đặt kênh, chính sách và kiểu khởi động được giữ nguyên. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm khả năng truy cập nguồn, xác thực, gửi từ xa, độ mới của sự kiện hay tình trạng đăng ký nhận.

### Nhật ký sự kiện Windows

Kiểm tra bốn kênh chẩn đoán về trạng thái bật, chế độ lưu giữ, kích thước hiện tại, giới hạn đã cấu hình, số bản ghi và mức đầy, sau đó kiểm tra tính khả dụng của dịch vụ Windows Event Log. Tự động sửa có thể khởi động dịch vụ đang bật đã dừng sau khi kiểm tra phụ thuộc và xác minh trạng thái đang chạy. Một thao tác chuyên biệt được xác nhận riêng có thể bật đúng một kênh chẩn đoán không được quản lý đang bị tắt, đồng thời giữ nguyên mọi cài đặt được báo cáo khác. Nhật ký, quyền kênh, lưu giữ, giới hạn kích thước, kiểu khởi động và cài đặt được quản lý được giữ nguyên. Việc gửi sự kiện thành công và dung lượng lưu trữ vẫn chưa được xác minh.

### Dịch vụ Thời gian Windows

Kiểm tra cấu hình, dịch vụ và đồng bộ hóa của Windows Time. Tự động sửa có thể khởi động W32Time khi trạng thái hiện tại báo đúng lỗi dịch vụ đã dừng và các kiểm tra mới về chính sách, chế độ và phụ thuộc đều đạt, sau đó yêu cầu đồng bộ lại và xác minh trạng thái mới. Thao tác này có thể điều chỉnh đồng hồ bằng các nguồn đã được cấu hình sẵn. Kiểu khởi động, cấu hình nguồn và cài đặt được quản lý được giữ nguyên.

### Dịch vụ Bộ lập lịch tác vụ

Kiểm kê có giới hạn trạng thái tác vụ đã lên lịch, kết quả chạy và số lần chạy bị lỡ, sau đó kết hợp chúng với lịch sử lỗi chính xác của dịch vụ Task Scheduler trước khi kiểm tra tính khả dụng hiện tại của dịch vụ. Kết luận về tác vụ và lỗi dịch vụ trong quá khứ không thể cho phép khởi động dịch vụ. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các tác vụ đang bật hiện có có thể bắt đầu dưới tài khoản đã cấu hình của chúng, kể cả các trình kích hoạt bị lỡ. Không có định nghĩa tác vụ, chủ thể, tham chiếu thông tin xác thực, chính sách hay kiểu khởi động nào bị thay đổi, và không có tác vụ nào được khởi động hay xóa trực tiếp. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm sự thành công của hành động, hiệu lực của thông tin xác thực hay kết quả tác vụ trong tương lai.

### Dịch vụ hồ sơ người dùng

Kiểm tra tính khả dụng của User Profile Service. Khôi phục tùy chọn chỉ khởi động đúng dịch vụ đang bật và đã dừng sau khi kiểm tra phụ thuộc mới. Các yêu cầu đăng nhập và hồ sơ hiện có có thể tiếp tục. Thư mục hồ sơ, tổ registry, quyền, đăng ký trong danh sách hồ sơ, thông tin xác thực, dữ liệu người dùng, chính sách, quyền dịch vụ và kiểu khởi động được giữ nguyên. Không có thao tác hồ sơ nào được yêu cầu. Việc xác minh chỉ bao gồm trạng thái ổn định của dịch vụ, không bao gồm đăng nhập, tải hồ sơ hay truy cập dữ liệu người dùng.

### Dịch vụ sao lưu

Kiểm tra các dịch vụ Volume Shadow Copy, Microsoft Software Shadow Copy Provider, Windows Backup và Block Level Backup Engine, sau đó đọc trạng thái trình ghi VSS, các loại nhà cung cấp đã đăng ký, ảnh chụp nhanh hiện tại và phân bổ bộ nhớ bản sao bóng. Các dịch vụ khởi động theo yêu cầu đã dừng có thể là bình thường khi nhàn rỗi. Khôi phục tùy chọn chỉ khởi động các dịch vụ tự động đã dừng sau khi kiểm tra phụ thuộc, sau đó xác minh trạng thái đang chạy. Công việc sao lưu đã lên lịch hoặc bị gián đoạn hiện có có thể tiếp tục. Cài đặt khởi động bị tắt và theo yêu cầu, ảnh chụp nhanh, bộ nhớ bản sao bóng, đăng ký nhà cung cấp, kế hoạch sao lưu, danh mục, lịch, đích, thông tin xác thực và thời hạn lưu giữ được giữ nguyên. Không có bản sao lưu nào được chọn hay cấu hình lại, không có dịch vụ nào được khởi động lại, và không có ảnh chụp nhanh hay danh mục nào bị xóa. Đăng ký nhà cung cấp, trạng thái trình ghi, phân bổ bộ nhớ và trạng thái ổn định của dịch vụ không xác minh việc sao lưu hoàn tất, quyền truy cập đích hay khôi phục thành công.
