import torch
import clip
from classes import SampleData, FirebaseDB
import firebase_setup as firedb
import json
from PIL import Image
from torchvision.transforms import Compose, Resize, CenterCrop, ToTensor, Normalize
import numpy as np
import heapq

def cuda_available():
    available = torch.cuda.is_available()
    # Выводим результат
    if available:
        return True
    else:
        return False
    
def take_db_data(sample_data):
    firebase_db = FirebaseDB(None, None)
    
    firebase_db.admin, firebase_db.app = firedb.initialize_connection()
    
    results = []
    
    ref = firebase_db.admin.reference(f'collection/{sample_data.year}/{sample_data.season}/')
    
    nested_items = ref.get()

    first_level_keys = list(nested_items.keys())
    
    for first_key in first_level_keys:
        first_key_ref = ref.child(f'{first_key}')
        nested_items = first_key_ref.get()
        
        second_level_keys = list(nested_items.keys())
        
        for second_key in second_level_keys:
            second_key_ref = first_key_ref.child(f'{second_key}/description')
            description = second_key_ref.get()

            if description is not None:
                path = f'{first_key}/{second_key}'
                results.append((path, description))
    
    firedb.drop_connection(firebase_db.app)
    
    with open("nested_items.txt", "w") as file:
        for path, description in results:
            file.write(f'{path}: {description}\n')
    
    # Обработка изображения с помощью функции recognition
    return recognition(results, sample_data.filepath)


def recognition(results, image_path):
    device = "cuda" if torch.cuda.is_available() else "cpu"
    neuro = "RN50x4"
    if neuro == "RN101":
        model, transform = clip.load("RN101", device=device)
    
        images = []
        texts = []
        path_to_desc = []
    
        text_features = {}
    
        image = Image.open(image_path).convert("RGB")
        preprocess = Compose([
            Resize(size=224, interpolation=Image.BICUBIC, max_size=None, antialias=None),
            CenterCrop(size=(224, 224)),
            ToTensor(),
            Normalize(mean=(0.48145466, 0.4578275, 0.40821073), std=(0.26862954, 0.26130258, 0.27577711)),
        ])
    
        images.append(preprocess(image))
    
        for path, desc in results:
            texts.append(desc)
            path_to_desc.append(path)
    
        text_tokens = clip.tokenize([desc for desc in texts]).to(device)
    
        with torch.no_grad():
            image_features = model.encode_image(torch.tensor(np.stack(images)).to(device)).float()
            text_features = model.encode_text(text_tokens).float()
    
        max_similarity = float('-inf')
        most_similar_description = None
        max_similarity_tokens = None
    
        image_features /= image_features.norm(dim=-1, keepdim=True)
        text_features /= text_features.norm(dim=-1, keepdim=True)
        similarity = text_features.cpu().numpy() @ image_features.cpu().numpy().T
    
        similarities = []
        min_heap = []

        for text_idx, image_idx in np.ndindex(similarity.shape):
            similarity_value = similarity[text_idx, image_idx].item()
            description_path = path_to_desc[text_idx]
    
            heapq.heappush(min_heap, (similarity_value, description_path))
    
            if len(min_heap) > 5:
                heapq.heappop(min_heap)

        for sim_value, desc_path in min_heap:
            similarities.append({
                "description_path": desc_path,
                "similarity": sim_value
            })
    
        return json.dumps(similarities)
    elif neuro == "RN50x4":
        # Загружаем модель и трансформации для CLIP
        model, transform = clip.load(neuro, device=device)

        # Инициализируем списки для хранения изображений, текстов и путей к описаниям
        images = []
        texts = []
        path_to_desc = []

        # Инициализируем словарь для текстовых признаков
        text_features = {}

        # Загружаем изображение и подготавливаем его для модели
        image = Image.open(image_path).convert("RGB")
        preprocess = Compose([
            Resize(size=288, interpolation=Image.BICUBIC, max_size=None, antialias=None),  # Изменение размера изображения до 288x288
            CenterCrop(size=(288, 288)),  # Центрирование и обрезка изображения до 288x288
            ToTensor(),  # Преобразование изображения в тензор
            Normalize(mean=(0.48145466, 0.4578275, 0.40821073), std=(0.26862954, 0.26130258, 0.27577711)),  # Нормализация тензора
        ])

        # Добавляем предобработанное изображение в список изображений
        images.append(preprocess(image).unsqueeze(0))

        # Проходим по списку результатов и извлекаем тексты и пути к описаниям
        for path, desc in results:
            texts.append(desc)
            path_to_desc.append(path)

        # Токенизируем тексты и загружаем их на устройство
        text_tokens = clip.tokenize([desc for desc in texts]).to(device)

        # Отключаем градиенты для ускорения вычислений в блоке с отключенным градиентом
        with torch.no_grad():
            # Объединяем изображения в один тензор и загружаем его на устройство
            image_tensor = torch.cat(images, dim=0).to(device)
            # Получаем признаки изображений и текстов с помощью модели CLIP
            image_features = model.encode_image(image_tensor).float()
            text_features = model.encode_text(text_tokens).float()

        # Нормализуем признаки изображений и текстов
        image_features /= image_features.norm(dim=-1, keepdim=True)
        text_features /= text_features.norm(dim=-1, keepdim=True)

        # Вычисляем матрицу сходства между текстовыми и визуальными признаками
        similarity = text_features.cpu().numpy() @ image_features.cpu().numpy().T

        # Инициализируем список сходств и минимальную кучу для поиска топ-5 сходств
        similarities = []
        min_heap = []

        # Проходим по всем комбинациям текстов и изображений и вычисляем сходства
        for text_idx, image_idx in np.ndindex(similarity.shape):
            similarity_value = similarity[text_idx, image_idx].item()
            description_path = path_to_desc[text_idx]
    
            # Добавляем текущее сходство в кучу
            heapq.heappush(min_heap, (similarity_value, description_path))
    
            # Если в куче больше 5 элементов, удаляем наименьший
            if len(min_heap) > 5:
                heapq.heappop(min_heap)

        # Проходим по минимальной куче и формируем список сходств для вывода
        for sim_value, desc_path in min_heap:
            similarities.append({
                "description_path": desc_path,
                "similarity": sim_value
            })

        # Возвращаем список сходств в формате JSON
        return json.dumps(similarities)
    else:
        model, transform = clip.load(neuro, device=device)
    
        images = []
        texts = []
        path_to_desc = []
    
        text_features = {}
    
        image = Image.open(image_path).convert("RGB")
        preprocess = Compose([
            Resize(size=384, interpolation=Image.BICUBIC, max_size=None, antialias=None),
            CenterCrop(size=(384, 384)),
            ToTensor(),
            Normalize(mean=(0.48145466, 0.4578275, 0.40821073), std=(0.26862954, 0.26130258, 0.27577711)),
        ])
    
        images.append(preprocess(image).unsqueeze(0))
    
        for path, desc in results:
            texts.append(desc)
            path_to_desc.append(path)
    
        text_tokens = clip.tokenize([desc for desc in texts]).to(device)
    
        with torch.no_grad():
            image_tensor = torch.cat(images, dim=0).to(device)  # Объединяем изображения в один тензор
            image_features = model.encode_image(image_tensor).float()
            text_features = model.encode_text(text_tokens).float()
    
        max_similarity = float('-inf')
        most_similar_description = None
        max_similarity_tokens = None
    
        image_features /= image_features.norm(dim=-1, keepdim=True)
        text_features /= text_features.norm(dim=-1, keepdim=True)
        similarity = text_features.cpu().numpy() @ image_features.cpu().numpy().T
    
        similarities = []
        min_heap = []

        for text_idx, image_idx in np.ndindex(similarity.shape):
            similarity_value = similarity[text_idx, image_idx].item()
            description_path = path_to_desc[text_idx]
    
            heapq.heappush(min_heap, (similarity_value, description_path))
    
            if len(min_heap) > 5:
                heapq.heappop(min_heap)

        for sim_value, desc_path in min_heap:
            similarities.append({
                "description_path": desc_path,
                "similarity": sim_value
            })
    
        return json.dumps(similarities)