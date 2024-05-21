import clip_model as clip_model

from classes import SampleData

from fastapi import FastAPI, File, UploadFile, HTTPException, Request
import fastapi.responses as _responses
import shutil
import os

app = FastAPI()

#Текстовое описание по которому нашла, Дата время, какой по счету образец?, фотки подгрузить, комент 10 дней
#Год, сезон, тема сезона, стиль - кортежи бд

#"at work": "yes, cancel, waiting, done",

@app.post("/files")
async def create_upload_file(request: Request):
    form = await request.form()
    
    season = form['season']
    year = form['year']
    file = form['file']
    
    data_folder = "Data"
    filename = f"{season}_{year}_{file.filename}"
    filepath = os.path.join(data_folder, filename)
    
    sample_data = SampleData(season, year, filename, filepath)
    
    with open(filepath, "wb") as buffer:
        buffer.write(await file.read())
        
    if clip_model.cuda_available():
        #most_similar_description, max_similarity = clip.take_db_data(sample_data)
        json = clip_model.take_db_data(sample_data)
        
    #return {"sample_data": str(sample_data), "most_similar_description": most_similar_description, "max_similarity": max_similarity}
    return json
"""
    try:
        # Создаем имя файла с учетом года и сезона
        filename = f"{year}_{season}_{file.filename}"
        
        # Сохраняем загруженное изображение на сервере
        with open(filename, "wb") as buffer:
            shutil.copyfileobj(file.file, buffer)
        
        # Возвращаем файловый поток с сохраненным изображением
        return _responses.FileResponse(filename, media_type="image/jpeg")
    except Exception as e:
        return {"error": str(e)}
"""
if __name__ == "__main__":
    import uvicorn
    from pyngrok import ngrok
    from classes import FirebaseDB 
    import firebase_setup as firedb
    
    firebase_db = FirebaseDB(None, None)
    firebase_db.admin, firebase_db.app = firedb.initialize_connection()
    
    https_tunnel = ngrok.connect(5080)
    
    tunnels = ngrok.get_tunnels()

    for tunnel in tunnels:
        if tunnel.proto == "https":
            https_url = tunnel.public_url
            https_url = https_url.split("//")[1]
            ref = firebase_db.admin.reference('/credentials/url')
            ref.set(https_url)
            break
    
    firedb.drop_connection(firebase_db.app)
    uvicorn.run(app, host="127.0.0.1", port=5080)