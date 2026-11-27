const User = require("./User");

async function findUserByEmail(email) {
    return User.findOne({
        email: email.toLowerCase()
    });
}

async function findUserById(id) {
    return User.findById(id);
}

async function createUser(user) {
    const newUser = new User(user);

    return newUser.save();
}

async function saveUser(user) {
    return user.save();
}

module.exports = {
    findUserByEmail,
    findUserById,
    createUser,
    saveUser
};