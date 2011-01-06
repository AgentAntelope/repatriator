import json
import string
import random
import hashlib
import threading
import shutil
from settings import settings
import os, sys
from logging import debug, warning, error

def _random_string(length):
    """
    returns a string of length length with random alphanumeric characters
    """
    chars = string.ascii_letters + string.digits
    code = ""
    for _ in range(length):
        code += chars[random.randint(0, len(chars)-1)]
    return code

def _hash_password(salt, password):
    encoded = (salt + password).encode('utf8')
    return hashlib.sha256(encoded).hexdigest()

def _build_path(dirname):
    try:
        os.makedirs(dirname)
        debug("made path %s" % dirname)
    except WindowsError as ex:
        pass

def _save_json():
    _lock.acquire()

    with open(_users_file, "w") as out:
        out.write(json.dumps(_users_data))

    _lock.release()

class Privilege:
    OperateHardware = 0
    ManageUsers = 1

class UserAlreadyExists(Exception):
    pass

class UserDoesNotExist(Exception):
    pass

class BadPassword(Exception):
    pass

class User:
    make_sure_these_keys_are_defined = [
        'salt',
        'password_hash',
        'privileges',
        'picture_folder',
        'bookmarks',
    ]
    def __init__(self, username, attrs):
        self.username = username
        self._attrs = attrs
        _build_path(self.picture_folder())

    def picture_folder(self):
        return self._attrs['picture_folder']

    def privileges(self):
        return self._attrs['privileges']

    def set_privileges(self, privileges):
        self._attrs['privileges'] = privileges

    def has_privilege(self, privilege):
        return privilege in self._attrs['privileges']

    def check_password(self, password):
        if _hash_password(self._attrs['salt'], password) != self._attrs['password_hash']:
            raise BadPassword

    def change_password(self, old_password, new_password):
        self.check_password(old_password)
        self.set_password(new_password)

    def set_password(self, new_password):
        self._attrs['salt'] = _random_string(32)
        self._attrs['password_hash'] = _hash_password(self._attrs['salt'], new_password)

    def bookmarks(self):
        return self._attrs['bookmarks']

    def set_bookmarks(self, bookmarks):
        self._attrs['bookmarks'] = bookmarks

    def check_all_attrs_are_defined(self):
        """
        can raise KeyError
        """
        for key in User.make_sure_these_keys_are_defined:
            self._attrs[key]

    def save(self):
        self.check_all_attrs_are_defined()
        _users_data[self.username] = self._attrs
        _save_json()

def get_user(username):
    """
    can raise UserDoesNotExist
    """
    try:
        user = User(username, _users_data[username])
        user.check_all_attrs_are_defined()
        return user
    except KeyError:
        raise UserDoesNotExist


def login(username, password):
    """
    can raise UserDoesNotExist or BadPassword.
    returns the User object if the authentication succeeded.
    """
    user = get_user(username)
    user.check_password(password)
    return user

def add_user(username, password, privileges):
    """
    can raise UserAlreadyExists. returns nothing.
    """
    if username in _users_data:
        raise UserAlreadyExists
    attrs = {
        'privileges': privileges,
        'picture_folder': os.path.join(
            settings['DATA_FOLDER'],
            hashlib.md5(username.encode('utf8')).hexdigest(),
        ),
        'bookmarks': [],
    }
    user = User(username, attrs)
    user.set_password(password)
    user.save()

def delete_user(username):
    """
    can raise UserDoesNotExist
    """
    user = get_user(username)
    if os.path.exists(user.picture_folder()):
        shutil.rmtree(user.picture_folder())
    del _users_data[username]
    _save_json()

def list_users():
    return _users_data

_users_file = os.path.join(settings['DATA_FOLDER'], "users.json")
_build_path(settings['DATA_FOLDER'])
_lock = threading.Lock()

try:
    with open(_users_file, "r") as f:
        try:
            _users_data = json.loads(f.read())
        except ValueError as ex:
            error("Corrupt users data, resetting users database.")
            raise IOError
except IOError as ex:
    _users_data = {}
    add_user("default_admin", "temp1234", [Privilege.ManageUsers])

