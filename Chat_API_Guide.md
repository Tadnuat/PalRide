# PalRide Chat API - Hướng dẫn sử dụng

## Tổng quan
API Chat cho phép tài xế và hành khách giao tiếp realtime trong các chuyến đi. Hệ thống sử dụng SignalR để gửi tin nhắn realtime và REST API để quản lý dữ liệu chat.

## Cấu hình SignalR

### Kết nối SignalR Hub
```javascript
// Kết nối đến SignalR Hub
const connection = new signalR.HubConnectionBuilder()
    .withUrl("/notificationHub", {
        accessTokenFactory: () => {
            return localStorage.getItem('token');
        }
    })
    .build();

// Bắt đầu kết nối
await connection.start();

// Tham gia nhóm user
await connection.invoke("JoinUserGroup");

// Tham gia nhóm chat cho một trip cụ thể
await connection.invoke("JoinChatGroup", tripId);
```

### Lắng nghe tin nhắn realtime
```javascript
// Nhận tin nhắn mới
connection.on("ReceiveChatMessage", (message) => {
    console.log("Tin nhắn mới:", message);
    // Hiển thị tin nhắn trong UI
    displayMessage(message);
});

// Nhận xác nhận tin nhắn đã gửi
connection.on("ChatMessageSent", (message) => {
    console.log("Tin nhắn đã gửi:", message);
    // Cập nhật UI để hiển thị tin nhắn đã gửi
});

// Nhận thông báo đang gõ
connection.on("ReceiveTyping", (typingData) => {
    console.log("Đang gõ:", typingData);
    // Hiển thị indicator "đang gõ"
    showTypingIndicator(typingData);
});

// Nhận thông báo tin nhắn đã đọc
connection.on("MessageRead", (readData) => {
    console.log("Tin nhắn đã đọc:", readData);
    // Cập nhật trạng thái đã đọc
});
```

## REST API Endpoints

### 1. Gửi tin nhắn
**POST** `/api/chat/send`

**Request Body:**
```json
{
    "tripId": 1,
    "toUserId": 2,
    "messageText": "Xin chào, tôi sẽ đến đúng giờ!"
}
```

**Response:**
```json
{
    "messageId": 1,
    "tripId": 1,
    "fromUserId": 1,
    "toUserId": 2,
    "messageText": "Xin chào, tôi sẽ đến đúng giờ!",
    "createdAt": "2024-01-15T10:30:00Z",
    "isRead": false,
    "fromUserName": "Nguyễn Văn A",
    "toUserName": "Trần Thị B"
}
```

### 2. Lấy lịch sử chat
**GET** `/api/chat/history/{tripId}`

**Response:**
```json
{
    "tripId": 1,
    "tripInfo": "Suối Tiên, Q. 9 → ĐH FPT, Q. 9. 12/08 20:00",
    "otherUserId": 2,
    "otherUserName": "Trần Thị B",
    "otherUserRole": "Passenger",
    "messages": [
        {
            "messageId": 1,
            "tripId": 1,
            "fromUserId": 1,
            "toUserId": 2,
            "messageText": "Xin chào!",
            "createdAt": "2024-01-15T10:30:00Z",
            "isRead": true,
            "fromUserName": "Nguyễn Văn A",
            "toUserName": "Trần Thị B"
        }
    ]
}
```

### 3. Lấy danh sách chat
**GET** `/api/chat/list`

**Response:**
```json
[
    {
        "tripId": 1,
        "tripInfo": "Suối Tiên, Q. 9 → ĐH FPT, Q. 9. 12/08 20:00",
        "otherUserId": 2,
        "otherUserName": "Trần Thị B",
        "otherUserRole": "Passenger",
        "lastMessage": "Cảm ơn bạn!",
        "lastMessageTime": "2024-01-15T10:35:00Z",
        "hasUnreadMessages": true,
        "unreadCount": 2
    }
]
```

### 4. Đánh dấu tin nhắn đã đọc
**POST** `/api/chat/mark-read`

**Request Body:**
```json
{
    "tripId": 1,
    "fromUserId": 2
}
```

### 5. Lấy số tin nhắn chưa đọc
**GET** `/api/chat/unread-count`

**Response:**
```json
{
    "unreadCount": 5
}
```

### 6. Gửi thông báo đang gõ
**POST** `/api/chat/typing`

**Request Body:**
```json
{
    "toUserId": 2,
    "tripId": 1,
    "isTyping": true
}
```

## Ví dụ sử dụng trong React Native

### Component Chat
```javascript
import React, { useState, useEffect } from 'react';
import { View, Text, TextInput, FlatList, TouchableOpacity } from 'react-native';
import * as signalR from '@microsoft/signalr';

const ChatScreen = ({ tripId, otherUser }) => {
    const [messages, setMessages] = useState([]);
    const [newMessage, setNewMessage] = useState('');
    const [connection, setConnection] = useState(null);
    const [isTyping, setIsTyping] = useState(false);

    useEffect(() => {
        initializeSignalR();
        loadChatHistory();
    }, []);

    const initializeSignalR = async () => {
        const token = await AsyncStorage.getItem('token');
        const newConnection = new signalR.HubConnectionBuilder()
            .withUrl('https://your-api.com/notificationHub', {
                accessTokenFactory: () => token
            })
            .build();

        // Lắng nghe tin nhắn mới
        newConnection.on('ReceiveChatMessage', (message) => {
            setMessages(prev => [...prev, message]);
        });

        // Lắng nghe thông báo đang gõ
        newConnection.on('ReceiveTyping', (typingData) => {
            setIsTyping(typingData.isTyping);
        });

        await newConnection.start();
        await newConnection.invoke('JoinUserGroup');
        await newConnection.invoke('JoinChatGroup', tripId);
        
        setConnection(newConnection);
    };

    const loadChatHistory = async () => {
        try {
            const response = await fetch(`/api/chat/history/${tripId}`, {
                headers: {
                    'Authorization': `Bearer ${await AsyncStorage.getItem('token')}`
                }
            });
            const data = await response.json();
            setMessages(data.messages);
        } catch (error) {
            console.error('Error loading chat history:', error);
        }
    };

    const sendMessage = async () => {
        if (!newMessage.trim()) return;

        try {
            const response = await fetch('/api/chat/send', {
                method: 'POST',
                headers: {
                    'Content-Type': 'application/json',
                    'Authorization': `Bearer ${await AsyncStorage.getItem('token')}`
                },
                body: JSON.stringify({
                    tripId: tripId,
                    toUserId: otherUser.userId,
                    messageText: newMessage
                })
            });

            if (response.ok) {
                setNewMessage('');
            }
        } catch (error) {
            console.error('Error sending message:', error);
        }
    };

    const sendTyping = async (typing) => {
        if (connection) {
            await connection.invoke('SendTyping', otherUser.userId, tripId, typing);
        }
    };

    return (
        <View style={styles.container}>
            <View style={styles.header}>
                <Text style={styles.headerTitle}>{otherUser.fullName}</Text>
                <Text style={styles.tripInfo}>Suối Tiên, Q. 9 → ĐH FPT, Q. 9. 12/08 20:00</Text>
            </View>

            <FlatList
                data={messages}
                keyExtractor={(item) => item.messageId.toString()}
                renderItem={({ item }) => (
                    <View style={[
                        styles.messageContainer,
                        item.fromUserId === currentUserId ? styles.sentMessage : styles.receivedMessage
                    ]}>
                        <Text style={styles.messageText}>{item.messageText}</Text>
                        <Text style={styles.messageTime}>
                            {new Date(item.createdAt).toLocaleTimeString()}
                        </Text>
                    </View>
                )}
            />

            {isTyping && (
                <Text style={styles.typingIndicator}>
                    {otherUser.fullName} đang gõ...
                </Text>
            )}

            <View style={styles.inputContainer}>
                <TextInput
                    style={styles.textInput}
                    value={newMessage}
                    onChangeText={setNewMessage}
                    onFocus={() => sendTyping(true)}
                    onBlur={() => sendTyping(false)}
                    placeholder="Viết tin nhắn"
                    multiline
                />
                <TouchableOpacity style={styles.sendButton} onPress={sendMessage}>
                    <Text style={styles.sendButtonText}>Gửi</Text>
                </TouchableOpacity>
            </View>
        </View>
    );
};

const styles = StyleSheet.create({
    container: {
        flex: 1,
        backgroundColor: '#f0f0f0',
    },
    header: {
        backgroundColor: '#2E7D32',
        padding: 16,
        paddingTop: 50,
    },
    headerTitle: {
        color: 'white',
        fontSize: 18,
        fontWeight: 'bold',
    },
    tripInfo: {
        color: 'white',
        fontSize: 14,
        marginTop: 4,
    },
    messageContainer: {
        margin: 8,
        padding: 12,
        borderRadius: 8,
        maxWidth: '80%',
    },
    sentMessage: {
        backgroundColor: '#4CAF50',
        alignSelf: 'flex-end',
    },
    receivedMessage: {
        backgroundColor: 'white',
        alignSelf: 'flex-start',
    },
    messageText: {
        fontSize: 16,
        color: '#333',
    },
    messageTime: {
        fontSize: 12,
        color: '#666',
        marginTop: 4,
    },
    typingIndicator: {
        padding: 8,
        fontStyle: 'italic',
        color: '#666',
    },
    inputContainer: {
        flexDirection: 'row',
        padding: 16,
        backgroundColor: '#2E7D32',
        alignItems: 'flex-end',
    },
    textInput: {
        flex: 1,
        backgroundColor: 'white',
        borderRadius: 20,
        paddingHorizontal: 16,
        paddingVertical: 8,
        marginRight: 8,
        maxHeight: 100,
    },
    sendButton: {
        backgroundColor: '#4CAF50',
        paddingHorizontal: 16,
        paddingVertical: 8,
        borderRadius: 20,
    },
    sendButtonText: {
        color: 'white',
        fontWeight: 'bold',
    },
});

export default ChatScreen;
```

## Bảo mật

1. **Authentication**: Tất cả API endpoints yêu cầu JWT token
2. **Authorization**: Chỉ tài xế và hành khách của chuyến đi mới có thể chat
3. **Rate Limiting**: Nên implement rate limiting để tránh spam
4. **Message Validation**: Kiểm tra độ dài và nội dung tin nhắn

## Lưu ý

1. **Connection Management**: Luôn đóng SignalR connection khi component unmount
2. **Error Handling**: Xử lý lỗi kết nối và gửi tin nhắn
3. **Offline Support**: Lưu tin nhắn local khi offline và sync khi online
4. **Performance**: Sử dụng pagination cho lịch sử chat dài


