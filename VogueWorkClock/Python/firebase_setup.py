import firebase_admin
from firebase_admin import credentials
from firebase_admin import db

def initialize_connection():
    cred_obj = firebase_admin.credentials.Certificate('your certificate')
    app = firebase_admin.initialize_app(cred_obj, {
        'databaseURL':"your database url"
    })
    return firebase_admin.db, app

def drop_connection(app):
    firebase_admin.delete_app(app)