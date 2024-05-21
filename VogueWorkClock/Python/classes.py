class SampleData:
    def __init__(self, season, year, filename, filepath):
        self.season = season
        self.year = year
        self.filename = filename
        self.filepath = filepath

    def __str__(self):
        return f"Season: {self.season}, Year: {self.year}, Filepath: {self.filepath}"
    
class FirebaseDB:
    def __init__(self, admin, app):
        self.admin = admin
        self.app = app
        